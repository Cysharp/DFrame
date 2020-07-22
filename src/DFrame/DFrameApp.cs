using ConsoleAppFramework;
using DFrame.Collections;
using DFrame.Internal;
using Grpc.Core;
using MagicOnion.Client;
using MagicOnion.Hosting;
using MagicOnion.Server;
using MessagePack;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Threading.Tasks;
using ZLogger;

namespace DFrame
{
    public static class DFrameAppHostBuilderExtensions
    {
        public static async Task RunDFrameAsync(this IHostBuilder hostBuilder, string[] args, DFrameOptions options)
        {
            var workerCollection = DFrameWorkerCollection.FromCurrentAssemblies();

            if (args.Length == 0)
            {
                ShowDFrameAppList(workerCollection);
                return;
            }

            hostBuilder = hostBuilder
                .ConfigureServices(x =>
                {
                    x.AddSingleton(options);
                    x.AddSingleton(workerCollection);

                    foreach (var item in workerCollection.All)
                    {
                        x.AddTransient(item.WorkerType);
                    }
                });

            if (args.Length != 0 && args.Contains("--worker-flag"))
            {
                await hostBuilder.RunConsoleAppFrameworkAsync<DFrameWorkerApp>(args);
            }
            else
            {
                await hostBuilder.RunConsoleAppFrameworkAsync<DFrameApp>(args);
            }
        }

        static void ShowDFrameAppList(DFrameWorkerCollection types)
        {
            Console.WriteLine("WorkerNames:");
            foreach (var item in types.All)
            {
                Console.WriteLine(item.Name);
            }
        }
    }

    internal class DFrameApp : ConsoleAppBase
    {
        ILogger<DFrameApp> logger;
        IServiceProvider provider;
        DFrameOptions options;
        DFrameWorkerCollection workers;
        IHost? masterHost;

        public DFrameApp(ILogger<DFrameApp> logger, IServiceProvider provider, DFrameOptions options, DFrameWorkerCollection workers)
        {
            this.provider = provider;
            this.logger = logger;
            this.workers = workers;
            this.options = options;
        }

        public async Task Main(
            string workerName,
            int processCount = 1,
            int workerPerProcess = 10,
            int executePerWorker = 10)
        {
            ThreadPoolUtility.SetMinThread(workerPerProcess);
            // validate worker is exists.
            if (!workers.TryGetWorker(workerName, out var _))
            {
                throw new InvalidOperationException($"Worker:{workerName} does not found in assembly.");
            }

            var failSignal = new TaskFailSignal();

            using (masterHost = StartMasterHost())
            await using (options.ScalingProvider)
            {
                var reporter = masterHost.Services.GetRequiredService<Reporter>();
                reporter.Reset(processCount);

                logger.LogInformation("Starting worker nodes.");
                await options.ScalingProvider.StartWorkerAsync(options, processCount, provider, failSignal, Context.CancellationToken).WithCancellation(Context.CancellationToken);

                await Task.WhenAny(reporter.OnConnected.Waiter.WithCancellation(Context.CancellationToken), failSignal.Task);

                var broadcaster = reporter.Broadcaster;

                logger.LogTrace("Send CreateWorker command to workers and wait complete message.");
                broadcaster.CreateCoWorker(workerPerProcess, workerName);
                await Task.WhenAny(reporter.OnCreateCoWorker.Waiter.WithCancellation(Context.CancellationToken), failSignal.Task);

                logger.LogTrace("Send Setup command to workers and wait complete message.");
                broadcaster.Setup();
                await Task.WhenAny(reporter.OnSetup.Waiter.WithCancellation(Context.CancellationToken), failSignal.Task);

                logger.LogTrace("Send Execute command to workers and wait complete message.");
                broadcaster.Execute(executePerWorker);
                await Task.WhenAny(reporter.OnExecute.Waiter.WithCancellation(Context.CancellationToken), failSignal.Task);

                logger.LogTrace("Send SetTeardownup command to workers and wait complete message.");
                broadcaster.Teardown();
                await Task.WhenAny(reporter.OnTeardown.Waiter.WithCancellation(Context.CancellationToken), failSignal.Task);

                options.OnExecuteResult?.Invoke(reporter.ExecuteResult.ToArray(), options, new ExecuteScenario(workerName, processCount, workerPerProcess, executePerWorker));

                broadcaster.Shutdown();

                await Task.Delay(TimeSpan.FromSeconds(1)); // wait Shutdown complete?
            }

            logger.LogInformation("Master shutdown.");
        }

        IHost StartMasterHost()
        {
            var host = options.HostBuilderFactory(Context.Arguments)
                .UseMagicOnion(targetTypes: new Type[]
                {
                    typeof(MasterHub),
                    typeof(DistributedQueueService),
                    typeof(DistributedStackService),
                    typeof(DistributedHashSetService),
                    typeof(DistributedListService),
                    typeof(IDistributedDictionaryService),
                }, options: new MagicOnionOptions
                {
                    IsReturnExceptionStackTraceInErrorDetail = true,
                    SerializerOptions = MessagePackSerializer.Typeless.DefaultOptions // use Typeless.
                }, ports: new ServerPort(options.MasterListenHost, options.MasterListenPort, ServerCredentials.Insecure),
                    new[] { 
                        // body message size
                        new ChannelOption("grpc.max_receive_message_length", int.MaxValue),
                        // keep alive
                        new ChannelOption("grpc.keepalive_time_ms", 2000),
                        new ChannelOption("grpc.keepalive_timeout_ms", 3000),
                        new ChannelOption("grpc.http2.min_time_between_pings_ms", 5000),
                    })
                .ConfigureServices(x =>
                {
                    x.AddSingleton<Reporter>();
                    x.AddSingleton(typeof(KeyedValueProvider<>));
                })
                .ConfigureLogging(logging =>
                {
                    logging.ClearProviders();
                    logging.AddZLoggerConsole(options =>
                    {
                        // todo: configure structured logging
                        options.EnableStructuredLogging = false;
                    });
                })
                .Build();

            logger.LogInformation("Starting DFrame master node.");

            var task = host.RunAsync(Context.CancellationToken);
            if (task.IsFaulted)
            {
                ExceptionDispatchInfo.Throw(task.Exception.InnerException);
            }

            return host;
        }
    }

    internal class DFrameWorkerApp : ConsoleAppBase
    {
        ILogger<DFrameWorkerApp> logger;
        IServiceProvider provider;
        DFrameOptions options;

        public DFrameWorkerApp(ILogger<DFrameWorkerApp> logger, IServiceProvider provider, DFrameOptions options)
        {
            this.provider = provider;
            this.logger = logger;
            this.options = options;
        }

        public async Task Main()
        {
            logger.LogInformation("Starting DFrame worker node");

            var channel = new Channel(options.WorkerConnectToHost, options.WorkerConnectToPort, ChannelCredentials.Insecure,
                new[] {
                    // keep alive
                    new ChannelOption("grpc.keepalive_time_ms", 2000),
                    new ChannelOption("grpc.keepalive_timeout_ms", 3000),
                    new ChannelOption("grpc.http2.min_time_between_pings_ms", 5000),
                });
            var nodeId = Guid.NewGuid();
            var receiver = new WorkerReceiver(channel, nodeId, provider);
            var client = StreamingHubClient.Connect<IMasterHub, IWorkerReceiver>(channel, receiver);
            receiver.Client = client;

            var disconnect = client.WaitForDisconnect();

            var t = await Task.WhenAny(client.ConnectCompleteAsync(nodeId), disconnect);
            if (t == disconnect)
            {
                await ShutdownAsync(client, channel, nodeId);
                return;
            }

            logger.LogInformation($"Worker -> Master connect successfully, WorkerNodeId:{nodeId.ToString()}.");
            try
            {
                // wait for shutdown command from master.
                await Task.WhenAny(receiver.WaitShutdown.WithCancellation(Context.CancellationToken), client.WaitForDisconnect());
            }
            finally
            {
                await ShutdownAsync(client, channel, nodeId);
            }
        }

        async Task ShutdownAsync(IMasterHub client, Channel channel, Guid nodeId)
        {
            logger.LogInformation($"Worker shutdown, WorkerNodeId:{nodeId.ToString()}.");

            logger.LogTrace($"Worker StreamingHubClient disposing.");
            await client.DisposeAsync();

            logger.LogTrace($"Worker Channel shutdown.");
            await channel.ShutdownAsync();
        }
    }

    public class CountReporting
    {
        readonly int max;
        int count;
        public Action<int>? OnIncrement;
        TaskCompletionSource<object?> waiter;

        public Task Waiter => waiter.Task;

        public CountReporting(int max)
        {
            this.max = max;
            this.OnIncrement = null;
            this.count = 0;
            this.waiter = new TaskCompletionSource<object?>(TaskCreationOptions.RunContinuationsAsynchronously);
        }

        public void IncrementCount()
        {
            var c = Interlocked.Increment(ref count);
            OnIncrement?.Invoke(c);
            if (c == max)
            {
                waiter.TrySetResult(default);
            }
        }

        public override string ToString()
        {
            return count.ToString();
        }
    }

    public class Reporter
    {
        int nodeCount;
        List<ExecuteResult> executeResult = new List<ExecuteResult>();

        public IReadOnlyList<ExecuteResult> ExecuteResult => executeResult;

        // global broadcaster of MasterHub.
        public IWorkerReceiver Broadcaster { get; set; } = default!;

        public CountReporting OnConnected { get; private set; } = default!;
        public CountReporting OnCreateCoWorker { get; private set; } = default!;
        public CountReporting OnSetup { get; private set; } = default!;
        public CountReporting OnExecute { get; private set; } = default!;
        public CountReporting OnTeardown { get; private set; } = default!;

        // Initialize
        public void Reset(int nodeCount)
        {
            this.nodeCount = nodeCount;
            this.OnConnected = new CountReporting(nodeCount);
            this.OnCreateCoWorker = new CountReporting(nodeCount);
            this.OnSetup = new CountReporting(nodeCount);
            this.OnExecute = new CountReporting(nodeCount);
            this.OnTeardown = new CountReporting(nodeCount);
        }

        public void AddExecuteResult(ExecuteResult[] results)
        {
            lock (executeResult)
            {
                executeResult.AddRange(results);
            }
        }
    }
}