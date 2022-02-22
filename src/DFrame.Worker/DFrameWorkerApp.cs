#nullable enable

using DFrame.Internal;
using Grpc.Core;
using Grpc.Net.Client;
using MagicOnion.Client;
using MessagePack;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace DFrame
{
    // worker-app work as a daemon, connect to controller at initialize and wait command.

    public class DFrameWorkerApp : ConsoleAppBase
#if UNITY_2020_1_OR_NEWER
        , IDisposable
#endif
    {
        DFrameWorkerEngine[] engines;

#if UNITY_2020_1_OR_NEWER

        CancellationTokenSource cancellationTokenSource = default!;

        public DFrameWorkerApp(string address)
            : this(new DFrameWorkerOptions(address))
        {
        }

        public DFrameWorkerApp(DFrameWorkerOptions options)
            : this(options, new IServiceProviderIsService(), null!)
        {
            this.cancellationTokenSource = new CancellationTokenSource();
            this.Context = new ConsoleAppContext()
            {
                CancellationToken = cancellationTokenSource.Token
            };
        }

        public void Dispose()
        {
            cancellationTokenSource.Cancel();
        }

#endif

#if UNITY_2020_1_OR_NEWER
        internal
#else
        public
#endif
            DFrameWorkerApp(DFrameWorkerOptions options, IServiceProviderIsService isService, IServiceProvider serviceProvider)
        {
            var workloadCollection = DFrameWorkloadCollection.FromAssemblies(options.WorkloadAssemblies, isService);
#if UNITY_2020_1_OR_NEWER
            var logger = new ILogger<DFrameWorkerEngine>();
#else
            var logger = serviceProvider.GetRequiredService<ILogger<DFrameWorkerEngine>>();
#endif

            this.engines = new DFrameWorkerEngine[Math.Max(1, options.VirtualProcess)];
            for (int i = 0; i < engines.Length; i++)
            {
                engines[i] = new DFrameWorkerEngine(logger, workloadCollection, options, isService, serviceProvider);
            }
        }

        [RootCommand]
        public async Task Run()
        {
            await Task.WhenAll(engines.Select(x => x.RunAsync(this.Context.CancellationToken)));
        }
    }

    internal class DFrameWorkerEngine : IWorkerReceiver
    {
        readonly ILogger<DFrameWorkerEngine> logger;
        readonly DFrameWorkerOptions options;
        readonly DFrameWorkloadCollection workloadCollection;
        readonly IServiceProvider serviceProvider;

        readonly WorkerId workerId;

        List<(WorkloadContext context, Workload workload)> workloads;
#if UNITY_2020_1_OR_NEWER
        Channel? channel;
#else
        GrpcChannel? channel;
#endif
        IControllerHub? client;

        int executionToken; // checker for current execution
        CancellationTokenSource? connectionLifeTime; // mix of WaitForDisconnect and Context.CancellationToken(App Lifetime)
        CancellationTokenSource? workloadLifeTime; // mix of ConnectionLifeTime + wait for stopped from controller

        TaskCompletionSource<ExecutionId> completeWorkloadSetup;
        TaskCompletionSource<object> completeExecute;
        TaskCompletionSource<object> completeTearDown;

        public DFrameWorkerEngine(ILogger<DFrameWorkerEngine> logger, DFrameWorkloadCollection workloads, DFrameWorkerOptions options, IServiceProviderIsService isService, IServiceProvider serviceProvider)
        {
            this.logger = logger;
            this.options = options;
            this.workloadCollection = workloads;
            this.serviceProvider = serviceProvider;
            this.workerId = new WorkerId(Guid.NewGuid());
            this.workloads = new List<(WorkloadContext context, Workload workload)>();
            this.completeWorkloadSetup = new TaskCompletionSource<ExecutionId>();
            this.completeExecute = new TaskCompletionSource<object>();
            this.completeTearDown = new TaskCompletionSource<object>();
        }

        public async Task RunAsync(CancellationToken applicationLifeTime)
        {
            logger.LogInformation($"Starting DFrame worker (WorkerId:{workerId})");

            var connectTimeout = options.ConnectTimeout;
            while (!applicationLifeTime.IsCancellationRequested)
            {
                try
                {
                    await ConnectAsync(applicationLifeTime);

                    while (!applicationLifeTime.IsCancellationRequested)
                    {
                        var waitFor = connectionLifeTime;
                        if (waitFor == null) throw new InvalidOperationException("ConnectionLifeTime cancellationToken is null");

                        logger.LogInformation($"Waiting command start.");
                        var executionId = await completeWorkloadSetup.Task.WaitAsync(waitFor.Token);

                        logger.LogInformation($"Complete workload setup, wait for execute start.");
                        await completeExecute.Task.WaitAsync(waitFor.Token);

                        logger.LogInformation($"Complete execute, wait for teardown start.");
                        await completeTearDown.Task.WaitAsync(waitFor.Token);
                    }
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Failed on worker executing.");
                    try
                    {
                        logger.LogInformation($"Teardown {workloads.Count} workload(s).");
                        await Task.WhenAll(workloads.Select(x => x.workload.InternalTeardownAsync(x.context)));
                        workloads.Clear();
                    }
                    catch (Exception ex2)
                    {
                        logger.LogError(ex2, "Error when workloads teardown.");
                    }

                    try
                    {
                        logger.LogInformation($"Shutdown connection.");
                        if (client != null) await client.DisposeAsync().WaitAsync(connectTimeout, applicationLifeTime);
                        if (channel != null)
                        {
                            await channel.ShutdownAsync();
#if !UNITY_2020_1_OR_NEWER
                            channel.Dispose();
#endif
                        }
                        if (workloadLifeTime != null)
                        {
                            workloadLifeTime.Cancel();
                            workloadLifeTime.Dispose();
                        }
                        if (connectionLifeTime != null)
                        {
                            connectionLifeTime.Cancel();
                            connectionLifeTime.Dispose();
                        }
                    }
                    catch (Exception ex2)
                    {
                        logger.LogError(ex2, "Error when client/channel disposing.");
                    }
                    client = null;
                    channel = null;
                    workloadLifeTime = null;
                    connectionLifeTime = null;

                    var reconnectTime = options.ReconnectTime;
                    logger.LogInformation($"Wait {reconnectTime} to reconnect.");
                    await Task.Delay(reconnectTime, applicationLifeTime);
                }
            }
        }

        // will call Connect completed or teardown completed
        void ResetSources()
        {
            this.executionToken = Interlocked.Increment(ref executionToken);
            this.workloadLifeTime = CancellationTokenSource.CreateLinkedTokenSource(connectionLifeTime!.Token);
            this.completeWorkloadSetup = new TaskCompletionSource<ExecutionId>();
            this.completeExecute = new TaskCompletionSource<object>();
            this.completeTearDown = new TaskCompletionSource<object>();
        }

        async Task ConnectAsync(CancellationToken applicationLifeTime)
        {
            var connectTimeout = options.ConnectTimeout;
            logger.LogInformation($"Start to connect Worker -> Controller. Address: {options.ControllerAddress}");

#if UNITY_2020_1_OR_NEWER
            channel = new Grpc.Core.Channel(options.ControllerAddress, options.GrpcChannelCredentials, options.GrpcChannelOptions);
#else
            channel = GrpcChannel.ForAddress(options.ControllerAddress, new GrpcChannelOptions
            {
                HttpHandler = new SocketsHttpHandler
                {
                    PooledConnectionIdleTimeout = options.SocketsHttpHandlerOptions.PooledConnectionIdleTimeout,
                    PooledConnectionLifetime = options.SocketsHttpHandlerOptions.PooledConnectionLifetime,
                    KeepAlivePingDelay = options.SocketsHttpHandlerOptions.KeepAlivePingDelay,
                    KeepAlivePingTimeout = options.SocketsHttpHandlerOptions.KeepAlivePingTimeout,
                    EnableMultipleHttp2Connections = true,
                    ConnectTimeout = connectTimeout,
                },
                LoggerFactory = this.serviceProvider.GetService<ILoggerFactory>()
            });
#endif

            var callInvoker = channel.CreateCallInvoker();
            var callOption = new CallOptions(new Metadata { { "worker-id", workerId.ToString() } });
            var connectTask = StreamingHubClient.ConnectAsync<IControllerHub, IWorkerReceiver>(callInvoker, this, option: callOption, serializerOptions:
#if UNITY_2020_1_OR_NEWER
                DFrameResolver.Options
#else
                MessagePackSerializerOptions.Standard
#endif
            );
            client = await connectTask.WaitAsync(connectTimeout);

            this.connectionLifeTime = CancellationTokenSource.CreateLinkedTokenSource(client!.WaitForDisconnect().ToCancellationToken(), applicationLifeTime);
            ResetSources();

            await client.ConnectAsync(workloadCollection.All.Select(x => x.WorkloadInfo).ToArray(), options.Metadata);

            logger.LogInformation($"Connect completed.");
        }

        async void IWorkerReceiver.CreateWorkloadAndSetup(ExecutionId executionId, int createCount, string workloadName, KeyValuePair<string, string>[] parameters)
        {
            var currentExecutionToken = executionToken;
            try
            {
                logger.LogInformation($"Creating {createCount} workload(s) of '{workloadName}', executionId: {executionId}");
                ThreadPoolUtility.SetMinThread(createCount);
                if (!workloadCollection.TryGetWorkload(workloadName, out var description))
                {
                    throw new InvalidOperationException($"Workload:{workloadName} does not found in assembly.");
                }

                workloads.Clear();
                for (int i = 0; i < createCount; i++)
                {
                    var workload = description.Activator.Value.Invoke(serviceProvider, description.CrateArgument(parameters));
                    var t = (new WorkloadContext(workloadLifeTime!.Token), (Workload)workload);
                    workloads.Add(t);
                }

                logger.LogInformation($"Instantiate {workloads.Count} workload(s) complete.");
                await Task.WhenAll(workloads.Select(x => x.workload.SetupAsync(x.context)));

                if (currentExecutionToken == executionToken)
                {
                    completeWorkloadSetup.TrySetResult(executionId);
                    await client!.CreateWorkloadCompleteAsync(executionId);
                }
            }
            catch (Exception ex)
            {
                if (currentExecutionToken == executionToken)
                {
                    completeWorkloadSetup.TrySetException(ex);
                }
                else
                {
                    logger.LogError(ex, "unhandled error in IWorkerReceiver.CreateWorkloadAndSetup");
                }
            }
        }

        async void IWorkerReceiver.Execute(long[] executeCount)
        {
            var currentExecutionToken = executionToken;
            try
            {
                logger.LogInformation($"Executing {workloads.Count} workload(s). (ExecutePerWorkload={executeCount.Max()})");
                var token = workloadLifeTime?.Token;
                if (token == null)
                {
                    throw new InvalidOperationException("Token is lost before invoke Execute.");
                }

                var completeResults = new Dictionary<WorkloadId, Dictionary<string, string>?>();
                try
                {
                    var isBatchReporting = options.MaxBatchRate > 1;

                    await Task.WhenAll(workloads.Select((x, workloadIndex) => Task.Run(async () =>
                    {
                        BatchedExecuteResult? batchResult = default;
                        if (isBatchReporting)
                        {
                            batchResult = new BatchedExecuteResult(x.context.WorkloadId, new List<long>(options.MaxBatchRate));
                        }
                        var batchRate = 0;
                        if (isBatchReporting)
                        {
                            batchRate = _Random.Shared.Next(options.MinBatchRate, options.MaxBatchRate);
                        }

                        var exec = executeCount[workloadIndex];
                        for (long i = 0; i < exec; i++)
                        {
                            x.context.CancellationToken.ThrowIfCancellationRequested();


                            string? errorMsg = null;
                            var sw = ValueStopwatch.StartNew();
                            try
                            {
                                await x.workload.ExecuteAsync(x.context);
                            }
                            catch (OperationCanceledException e) when (e.CancellationToken == token.Value)
                            {
                                return;
                            }
                            catch (Exception ex)
                            {
                                errorMsg = ex.ToString();
                            }

                            if (!isBatchReporting || errorMsg != null)
                            {
                                var executeResult = new ExecuteResult(x.context.WorkloadId, sw.Elapsed, i, (errorMsg != null), errorMsg);
                                await client!.ReportProgressAsync(executeResult);
                            }
                            else if (batchResult != null)
                            {
                                batchResult.BatchedElapsed.Add(sw.ElapsedTicks);
                                if (batchResult.BatchedElapsed.Count >= batchRate)
                                {
                                    await client!.ReportProgressBatchedAsync(batchResult);
                                    batchRate = _Random.Shared.Next(options.MinBatchRate, options.MaxBatchRate);
                                    batchResult.BatchedElapsed.Clear();
                                }
                            }
                        }

                        if (batchResult != null && batchResult.BatchedElapsed.Count > 0)
                        {
                            await client!.ReportProgressBatchedAsync(batchResult);
                        }

                        lock (completeResults)
                        {
                            completeResults[x.context.WorkloadId] = x.workload.Complete();
                        }
                    }, token.Value)));
                }
                catch (OperationCanceledException e) when (e.CancellationToken == token.Value)
                {
                }

                if (currentExecutionToken == executionToken)
                {
                    completeExecute.TrySetResult(null!); // call complete before ExecuteCompleteAsync
                    await client!.ExecuteCompleteAsync(completeResults);
                }
            }
            catch (Exception ex)
            {
                if (currentExecutionToken == executionToken)
                {
                    completeExecute.TrySetException(ex);
                }
                else
                {
                    logger.LogError(ex, "unhandled error in IWorkerReceiver.Execute");
                }
            }
        }

        void IWorkerReceiver.Stop()
        {
            logger.LogInformation($"Received stop request.");
            var tokenSource = this.workloadLifeTime;
            if (tokenSource != null)
            {
                tokenSource.Cancel();
            }
        }

        async void IWorkerReceiver.Teardown()
        {
            var complete = completeTearDown;
            try
            {
                logger.LogInformation($"Teardown {workloads.Count} workload(s).");
                await Task.WhenAll(workloads.Select(x => x.workload.InternalTeardownAsync(x.context)));
                workloads.Clear();

                ResetSources(); // reset field before send complete to server.

                await client!.TeardownCompleteAsync();
            }
            catch (Exception ex)
            {
                complete.TrySetException(ex);
                return;
            }

            complete.TrySetResult(null!); // call stored TCS
        }
    }

#if !UNITY_2020_1_OR_NEWER

    // to share with Unity.
    internal static class _Random
    {
        internal static Random Shared => Random.Shared;
    }

#endif
}