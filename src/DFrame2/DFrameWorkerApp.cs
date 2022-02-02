using DFrame.Internal;
using Grpc.Core;
using Grpc.Net.Client;
using MagicOnion.Client;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Collections.Immutable;

namespace DFrame;

// worker-app work as a daemon, connect to controller at initialize and wait command.

internal class DFrameWorkerApp : ConsoleAppBase, IWorkerReceiver
{
    readonly ILogger<DFrameWorkerApp> logger;
    readonly DFrameOptions options;
    readonly DFrameWorkloadCollection workloadCollection;
    readonly IServiceProvider serviceProvider;

    readonly Guid workerId;

    List<(WorkloadContext context, Workload workload)> workloads;
    GrpcChannel? channel;
    IControllerHub? client;
    TaskCompletionSource<Guid> completeWorkloadSetup;
    TaskCompletionSource completeExecute;
    TaskCompletionSource completeTearDown;

    public DFrameWorkerApp(ILogger<DFrameWorkerApp> logger, DFrameOptions options, DFrameWorkloadCollection workloadCollection, IServiceProvider serviceProvider)
    {
        this.logger = logger;
        this.options = options;
        this.workloadCollection = workloadCollection;
        this.serviceProvider = serviceProvider;
        this.workerId = Guid.NewGuid();
        this.workloads = new List<(WorkloadContext context, Workload workload)>();
        this.completeWorkloadSetup = new TaskCompletionSource<Guid>();
        this.completeExecute = new TaskCompletionSource();
        this.completeTearDown = new TaskCompletionSource();
    }

    [RootCommand]
    public async Task Run()
    {
        logger.LogInformation($"Starting DFrame worker (WorkerId:{workerId})");

        var connectTimeout = TimeSpan.FromSeconds(30); // TODO:options.ConnectTimeout,
        while (!Context.CancellationToken.IsCancellationRequested)
        {
            try
            {
                await ConnectAsync();
                var waitFor = CancellationTokenSource.CreateLinkedTokenSource(client!.WaitForDisconnect().ToCancellationToken(), Context.CancellationToken);
                while (!Context.CancellationToken.IsCancellationRequested)
                {
                    logger.LogInformation($"Waiting command start.");
                    var executionId = await completeWorkloadSetup.Task.WaitAsync(waitFor.Token);

                    logger.LogInformation($"Complete workload setup, wait for execute start.");
                    await completeExecute.Task.WaitAsync(waitFor.Token);

                    logger.LogInformation($"Complete execute, wait for teardown start.");
                    await completeTearDown.Task.WaitAsync(waitFor.Token);

                    this.completeWorkloadSetup = new TaskCompletionSource<Guid>();
                    this.completeExecute = new TaskCompletionSource();
                    this.completeTearDown = new TaskCompletionSource();
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed on worker executing.");
                try
                {
                    if (client != null) await client.DisposeAsync().WaitAsync(connectTimeout, Context.CancellationToken);
                    if (channel != null)
                    {
                        await channel.ShutdownAsync();
                        channel.Dispose();
                    }
                }
                catch (Exception ex2)
                {
                    logger.LogError(ex2, "Error when client/channel disposing.");
                }
                client = null;
                channel = null;
                this.completeWorkloadSetup = new TaskCompletionSource<Guid>();
                this.completeExecute = new TaskCompletionSource();
                this.completeTearDown = new TaskCompletionSource();

                logger.LogInformation("Wait 5 seconds to reconnect.");
                await Task.Delay(TimeSpan.FromSeconds(5), Context.CancellationToken);
            }
        }
    }

    async Task ConnectAsync()
    {
        var connectTimeout = TimeSpan.FromSeconds(30); // TODO:options.Timeout,
        var address = "http://" + options.WorkerConnectToHost + ":" + options.WorkerConnectToPort;
        logger.LogInformation($"Start to connect Worker -> Controller. Address: {address}");

        channel = GrpcChannel.ForAddress("http://" + options.WorkerConnectToHost + ":" + options.WorkerConnectToPort, new GrpcChannelOptions
        {
            HttpHandler = new SocketsHttpHandler
            {
                PooledConnectionIdleTimeout = Timeout.InfiniteTimeSpan,
                KeepAlivePingDelay = TimeSpan.FromSeconds(60),
                KeepAlivePingTimeout = TimeSpan.FromSeconds(30),
                EnableMultipleHttp2Connections = true,
                ConnectTimeout = connectTimeout,
            },

            LoggerFactory = this.serviceProvider.GetService<ILoggerFactory>()
        });

        var callInvoker = channel.CreateCallInvoker();
        var callOption = new CallOptions(new Metadata { { "worker-id", workerId.ToString() } });
        var connectTask = StreamingHubClient.ConnectAsync<IControllerHub, IWorkerReceiver>(callInvoker, this, option: callOption, serializerOptions: options.SerializerOptions);
        client = await connectTask.WaitAsync(connectTimeout);

        logger.LogInformation($"Connect completed.");
    }

    async void IWorkerReceiver.CreateWorkloadAndSetup(Guid executionId, int createCount, string workloadName)
    {
        try
        {
            logger.LogInformation($"Creating {createCount} workload(s) of '{workloadName}', executionId: {executionId}");
            ThreadPoolUtility.SetMinThread(createCount);
            if (!workloadCollection.TryGetWorkload(workloadName, out var description))
            {
                // TODO:send error log to master.
                throw new InvalidOperationException($"Workload:{workloadName} does not found in assembly.");
            }

            workloads.Clear();
            for (int i = 0; i < createCount; i++)
            {
                var workload = serviceProvider.GetRequiredService(description.WorkloadType);
                var t = (new WorkloadContext(channel!, options), (Workload)workload);
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
        // TODO:add progress...
        //var progress = workloads.Length * executeCount / 10;
        //var increment = 0;

        try
        {
            logger.LogInformation($"Executing {workloads.Count} workload(s). (ExecutePerWorkload={executeCount})");

            var result = await Task.WhenAll(workloads.Select(x => Task.Run(async () =>
            {
                var list = new List<ExecuteResult>(executeCount);
                for (int i = 0; i < executeCount; i++)
                {
                    string? errorMsg = null;
                    var sw = ValueStopwatch.StartNew();
                    try
                    {
                        await x.workload.ExecuteAsync(x.context);
                    }
                    catch (Exception ex)
                    {
                        errorMsg = ex.ToString();
                    }

                    var executeResult = new ExecuteResult(x.context.WorkloadId, sw.Elapsed, i, (errorMsg != null), errorMsg);
                    list.Add(executeResult);

                    // TODO:progress?
                    // _ = Task.Run(() => client!.ReportProgressAsync(executeResult));
                }
                return list;
            })));

            // TODO:send result? should summarize?
            await client!.ExecuteCompleteAsync(result.SelectMany(xs => xs).ToArray());
            completeExecute.TrySetResult();
        }
        catch (Exception ex)
        {
            completeExecute.TrySetException(ex);
        }
    }

    // for RampUp - Execute
    void IWorkerReceiver.ExecuteUntilReceiveStop()
    {
        //    logger.LogInformation($"Executing workload(s) until a stop request is received.");
        //    while (!receiveStopped)
        //    {
        //        await Task.WhenAll(workloads.Select(x => Task.Run(async () =>
        //        {
        //            string? errorMsg = null;
        //            var sw = ValueStopwatch.StartNew();
        //            try
        //            {
        //                await x.workload.ExecuteAsync(x.context);
        //            }
        //            catch (Exception ex)
        //            {
        //                errorMsg = ex.ToString();
        //            }

        //            var executeResult = new ExecuteResult(x.context.WorkloadId, sw.Elapsed, 0, (errorMsg != null), errorMsg);
        //            // TODO:toriaezu
        //            _ = Task.Run(() => Client.ReportProgressAsync(executeResult));
        //        })));
        //    }

        //    await Client.ExecuteCompleteAsync(new ExecuteResult[0]); // TODO:use others.
    }

    // for RampUp - Stop
    void IWorkerReceiver.Stop()
    {
        throw new NotImplementedException();
    }

    async void IWorkerReceiver.Teardown()
    {
        try
        {
            await Task.WhenAll(workloads.Select(x => x.workload.TeardownAsync(x.context)));
            await client!.TeardownCompleteAsync();
            completeTearDown.TrySetResult();
        }
        catch (Exception ex)
        {
            completeTearDown.TrySetException(ex);
        }
    }
}