using DFrame.Internal;
using Grpc.Core;
using Grpc.Net.Client;
using MagicOnion.Client;
using MessagePack;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace DFrame;

// worker-app work as a daemon, connect to controller at initialize and wait command.

internal class DFrameWorkerApp : ConsoleAppBase, IWorkerReceiver
{
    readonly ILogger<DFrameWorkerApp> logger;
    readonly DFrameOptions options;
    readonly DFrameWorkloadCollection workloadCollection;
    readonly IServiceProvider serviceProvider;

    readonly WorkerId workerId;

    List<(WorkloadContext context, Workload workload)> workloads;
    GrpcChannel? channel;
    IControllerHub? client;
    CancellationTokenSource? cancellationTokenSource;
    TaskCompletionSource<ExecutionId> completeWorkloadSetup;
    TaskCompletionSource completeExecute;
    TaskCompletionSource completeTearDown;

    public DFrameWorkerApp(ILogger<DFrameWorkerApp> logger, DFrameOptions options, IServiceProviderIsService isService, IServiceProvider serviceProvider)
    {
        this.logger = logger;
        this.options = options;
        this.workloadCollection = DFrameWorkloadCollection.FromAssemblies(options.WorkloadAssemblies, isService);
        this.serviceProvider = serviceProvider;
        this.workerId = new WorkerId(Guid.NewGuid());
        this.workloads = new List<(WorkloadContext context, Workload workload)>();
        this.completeWorkloadSetup = new TaskCompletionSource<ExecutionId>();
        this.completeExecute = new TaskCompletionSource();
        this.completeTearDown = new TaskCompletionSource();
    }

    [RootCommand]
    public async Task Run()
    {
        logger.LogInformation($"Starting DFrame worker (WorkerId:{workerId})");

        var connectTimeout = options.ConnectTimeout;
        while (!Context.CancellationToken.IsCancellationRequested)
        {
            try
            {
                await ConnectAsync();

                using var waitFor = CancellationTokenSource.CreateLinkedTokenSource(client!.WaitForDisconnect().ToCancellationToken(), Context.CancellationToken);
                while (!Context.CancellationToken.IsCancellationRequested)
                {
                    // This token is used in WorkloadContext and Execute Task.Run.
                    this.cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(waitFor.Token);

                    logger.LogInformation($"Waiting command start.");
                    var executionId = await completeWorkloadSetup.Task.WaitAsync(waitFor.Token);

                    logger.LogInformation($"Complete workload setup, wait for execute start.");
                    await completeExecute.Task.WaitAsync(waitFor.Token);

                    logger.LogInformation($"Complete execute, wait for teardown start.");
                    await completeTearDown.Task.WaitAsync(waitFor.Token);

                    cancellationTokenSource.Cancel();
                    cancellationTokenSource.Dispose();
                    cancellationTokenSource = null;
                    this.completeWorkloadSetup = new TaskCompletionSource<ExecutionId>();
                    this.completeExecute = new TaskCompletionSource();
                    this.completeTearDown = new TaskCompletionSource();
                    workloads.Clear();
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
                    if (client != null) await client.DisposeAsync().WaitAsync(connectTimeout, Context.CancellationToken);
                    if (channel != null)
                    {
                        await channel.ShutdownAsync();
                        channel.Dispose();
                    }
                    if (cancellationTokenSource != null)
                    {
                        cancellationTokenSource.Cancel();
                        cancellationTokenSource.Dispose();
                    }
                }
                catch (Exception ex2)
                {
                    logger.LogError(ex2, "Error when client/channel disposing.");
                }
                client = null;
                channel = null;
                cancellationTokenSource = null;
                this.completeWorkloadSetup = new TaskCompletionSource<ExecutionId>();
                this.completeExecute = new TaskCompletionSource();
                this.completeTearDown = new TaskCompletionSource();

                var reconnectTime = options.ReconnectTime;
                logger.LogInformation($"Wait {reconnectTime} to reconnect.");
                await Task.Delay(reconnectTime, Context.CancellationToken);
            }
        }
    }

    async Task ConnectAsync()
    {
        var connectTimeout = options.ConnectTimeout;
        logger.LogInformation($"Start to connect Worker -> Controller. Address: {options.ControllerAddress}");

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

        var callInvoker = channel.CreateCallInvoker();
        var callOption = new CallOptions(new Metadata { { "worker-id", workerId.ToString() } });
        var connectTask = StreamingHubClient.ConnectAsync<IControllerHub, IWorkerReceiver>(callInvoker, this, option: callOption, serializerOptions: MessagePackSerializerOptions.Standard);
        client = await connectTask.WaitAsync(connectTimeout);

        await client.InitializeMetadataAsync(workloadCollection.All.Select(x => x.WorkloadInfo).ToArray(), options.Metadata);

        logger.LogInformation($"Connect completed.");
    }

    async void IWorkerReceiver.CreateWorkloadAndSetup(ExecutionId executionId, int createCount, string workloadName, (string name, string value)[] parameters)
    {
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
                var t = (new WorkloadContext(cancellationTokenSource!.Token), (Workload)workload);
                workloads.Add(t);
            }

            await Task.WhenAll(workloads.Select(x => x.workload.SetupAsync(x.context)));

            await client!.CreateWorkloadCompleteAsync(executionId);
            completeWorkloadSetup.TrySetResult(executionId);
        }
        catch (Exception ex)
        {
            completeWorkloadSetup.TrySetException(ex);
        }
    }

    async void IWorkerReceiver.Execute(int executeCount)
    {
        try
        {
            logger.LogInformation($"Executing {workloads.Count} workload(s). (ExecutePerWorkload={executeCount})");
            var token = cancellationTokenSource?.Token;
            if (token == null)
            {
                throw new InvalidOperationException("Token is lost before invoke Execute.");
            }

            try
            {
                await Task.WhenAll(workloads.Select(x => Task.Run(async () =>
                {
                    for (int i = 0; i < executeCount; i++)
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

                        var executeResult = new ExecuteResult(x.context.WorkloadId, sw.Elapsed, i, (errorMsg != null), errorMsg);
                        await client!.ReportProgressAsync(executeResult);
                    }
                }, token.Value)));
            }
            catch (OperationCanceledException e) when (e.CancellationToken == token.Value)
            {
            }

            await client!.ExecuteCompleteAsync();
            completeExecute.TrySetResult();
        }
        catch (Exception ex)
        {
            completeExecute.TrySetException(ex);
        }
    }

    async void IWorkerReceiver.ExecuteUntilReceiveStop()
    {
        try
        {
            logger.LogInformation($"Executing {workloads.Count} workload(s).");
            var token = cancellationTokenSource?.Token;
            if (token == null)
            {
                throw new InvalidOperationException("Token is lost before invoke ExecuteUntilReceiveStop.");
            }

            try
            {
                await Task.WhenAll(workloads.Select(x => Task.Run(async () =>
                {
                    var i = 0;
                    while (!token.Value.IsCancellationRequested)
                    {
                        string? errorMsg = null;
                        var sw = ValueStopwatch.StartNew();
                        try
                        {
                            await x.workload.ExecuteAsync(x.context);
                        }
                        catch (OperationCanceledException e) when (e.CancellationToken == token)
                        {
                            // complete.
                            return;
                        }
                        catch (Exception ex)
                        {
                            errorMsg = ex.ToString();
                        }

                        var executeResult = new ExecuteResult(x.context.WorkloadId, sw.Elapsed, i, (errorMsg != null), errorMsg);
                        await client!.ReportProgressAsync(executeResult);
                        i++;
                    }
                }, token.Value)));
            }
            catch (OperationCanceledException e) when (e.CancellationToken == token)
            {
            }

            await client!.ExecuteCompleteAsync();
            completeExecute.TrySetResult();
        }
        catch (Exception ex)
        {
            completeExecute.TrySetException(ex);
        }
    }

    void IWorkerReceiver.Stop()
    {
        logger.LogInformation($"Received stop request.");
        var tokenSource = this.cancellationTokenSource;
        if (tokenSource != null)
        {
            tokenSource.Cancel();
        }
    }

    async void IWorkerReceiver.Teardown()
    {
        try
        {
            logger.LogInformation($"Teardown {workloads.Count} workload(s).");
            await Task.WhenAll(workloads.Select(x => x.workload.InternalTeardownAsync(x.context)));
            await client!.TeardownCompleteAsync();
            completeTearDown.TrySetResult();
        }
        catch (Exception ex)
        {
            completeTearDown.TrySetException(ex);
        }
    }
}