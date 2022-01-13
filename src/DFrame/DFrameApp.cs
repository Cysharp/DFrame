using ConsoleAppFramework;
using DFrame.Collections;
using DFrame.Internal;
using Grpc.Core;
using Grpc.Net.Client;
using MagicOnion.Client;
using MagicOnion.Server;
using MessagePack;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
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
            var workloadCollection = DFrameWorkloadCollection.FromCurrentAssemblies();

            if (args.Length == 0)
            {
                ShowDFrameAppList(workloadCollection);
                return;
            }

            var errorHolder = new ExceptionHoldLoggerProvider();

            hostBuilder = hostBuilder
                .ConfigureServices(x =>
                {
                    x.AddSingleton(options);
                    x.AddSingleton(workloadCollection);

                    foreach (var item in workloadCollection.All)
                    {
                        x.AddTransient(item.WorkloadType);
                    }
                })
                .ConfigureLogging(x =>
                {
                    if (options.ConfigureInnerHostLogging != null)
                    {
                        options.ConfigureInnerHostLogging(x);
                    }

                    x.AddProvider(errorHolder);
                });

            if (args.Length != 0 && args.Contains("--worker-flag"))
            {
                await hostBuilder.RunConsoleAppFrameworkAsync<DFrameWorkerApp>(args);
            }
            else
            {
                await hostBuilder.RunConsoleAppFrameworkAsync<DFrameApp>(args);
            }

            if (errorHolder.Exception != null)
            {
                ExceptionDispatchInfo.Throw(errorHolder.Exception);
            }
        }

        static void ShowDFrameAppList(DFrameWorkloadCollection types)
        {
            Console.WriteLine("Workloads:");
            foreach (var item in types.All)
            {
                Console.WriteLine(item.Name);
            }
        }

        class ExceptionHoldLoggerProvider : ILoggerProvider
        {
            public Exception? Exception { get; set; }

            public ILogger CreateLogger(string categoryName)
            {
                return new Logger(this);
            }

            public void Dispose()
            {
            }

            class Logger : ILogger, IDisposable
            {
                ExceptionHoldLoggerProvider parent;

                public Logger(ExceptionHoldLoggerProvider parent)
                {
                    this.parent = parent;
                }

                public IDisposable BeginScope<TState>(TState state)
                {
                    return this;
                }

                public void Dispose()
                {
                }

                public bool IsEnabled(LogLevel logLevel)
                {
                    return true;
                }

                public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
                {
                    if (exception != null)
                    {
                        parent.Exception = exception;
                    }
                }
            }
        }
    }

    internal class DFrameApp : ConsoleAppBase
    {
        readonly ILogger<DFrameApp> logger;
        readonly IServiceProvider provider;
        readonly DFrameOptions options;
        readonly DFrameWorkloadCollection workers;

        public DFrameApp(ILogger<DFrameApp> logger, IServiceProvider provider, DFrameOptions options, DFrameWorkloadCollection workers)
        {
            this.provider = provider;
            this.logger = logger;
            this.workers = workers;
            this.options = options;
        }

        [Command("batch")]
        public Task ExecuteAsBatch(
            string workloadName,
            int workerCount = 1)
        {
            return ExecuteAsConcurrentRequest(workloadName, workerCount, 1, 1);
        }

        [Command("request")]
        public Task ExecuteAsConcurrentRequest(
            string workloadName,
            int workerCount,
            int workloadPerWorker,
            int executePerWorkload)
        {
            return new DFrameConcurrentRequestRunner(logger, provider, options, workers, workloadPerWorker, executePerWorkload).RunAsync(workloadName, workerCount, workloadPerWorker, executePerWorkload, this.Context);
        }

        [Command("rampup")]
        public Task ExecuteAsRampup(
            string workloadName,
            int workerCount,
            int maxWorkloadPerWorker,
            int workloadSpawnCount,
            int workloadSpawnSecond
            )
        {
            return new DFrameRamupRunner(logger, provider, options, workers, maxWorkloadPerWorker, workloadSpawnCount, workloadSpawnSecond).RunAsync(workloadName, workerCount, maxWorkloadPerWorker, maxWorkloadPerWorker, this.Context);
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
            logger.LogInformation("Starting DFrame worker");

            var channel = GrpcChannel.ForAddress("http://" + options.WorkerConnectToHost + ":" + options.WorkerConnectToPort, new GrpcChannelOptions
            {
                //HttpClient = new HttpClient(new SocketsHttpHandler
                //{
                //    ConnectTimeout = options.Timeout
                //}),

                HttpHandler = new SocketsHttpHandler
                {
                    PooledConnectionIdleTimeout = Timeout.InfiniteTimeSpan,
                    KeepAlivePingDelay = TimeSpan.FromSeconds(60),
                    KeepAlivePingTimeout = TimeSpan.FromSeconds(30),
                    EnableMultipleHttp2Connections = true,
                    ConnectTimeout = TimeSpan.FromSeconds(1), // TODO:options.Timeout,
                },

                LoggerFactory = this.provider.GetService<ILoggerFactory>()
            });



            var callInvoker = channel.CreateCallInvoker();

            var workerId = Guid.NewGuid();
            var receiver = new WorkerReceiver(channel, workerId, provider, options);
            var callOption = new CallOptions(new Metadata { { "worker-id", workerId.ToString() } });

            var client = await StreamingHubClient.ConnectAsync<IMasterHub, IWorkerReceiver>(callInvoker, receiver, option: callOption, serializerOptions: options.SerializerOptions);
            // Connect explicitly???
            receiver.Client = client;

            await client.ConnectAsync();

            logger.LogInformation($"Worker -> Master connect completed successfully, WorkerId:{workerId.ToString()}.");
            try
            {
                // wait for shutdown command from master.
                await Task.WhenAny(receiver.WaitShutdown.WithCancellation(Context.CancellationToken), client.WaitForDisconnect());
            }
            finally
            {
                await ShutdownAsync(client, channel, workerId);
            }
        }

        async Task ShutdownAsync(IMasterHub client, GrpcChannel channel, Guid workerId)
        {
            logger.LogInformation($"Worker shutdown, WorkerId:{workerId.ToString()}.");

            logger.LogTrace($"Worker StreamingHubClient disposing.");
            await client.DisposeAsync();

            logger.LogTrace($"Worker Channel shutdown.");
            await channel.ShutdownAsync();
        }
    }

    // TODO:will remove there.

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
        int workerCount;
        List<ExecuteResult> executeResult = new List<ExecuteResult>();

        public IReadOnlyList<ExecuteResult> ExecuteResult => executeResult;

        // global broadcaster of MasterHub.
        public IWorkerReceiver Broadcaster { get; set; } = default!;

        public CountReporting OnConnected { get; private set; } = default!;
        public CountReporting OnCreateWorkload { get; private set; } = default!;
        public CountReporting OnSetup { get; private set; } = default!;
        public CountReporting OnExecute { get; private set; } = default!;
        public CountReporting OnTeardown { get; private set; } = default!;

        // Initialize
        public void Reset(int workerCount)
        {
            this.workerCount = workerCount;
            this.OnConnected = new CountReporting(workerCount)
            {
                OnIncrement = count => WorkerProgressNotifier.OnConnected.PublishAsync(count).ConfigureAwait(false),
            };
            this.OnCreateWorkload = new CountReporting(workerCount);
            this.OnSetup = new CountReporting(workerCount);
            this.OnExecute = new CountReporting(workerCount);
            this.OnTeardown = new CountReporting(workerCount)
            {
                OnIncrement = count => WorkerProgressNotifier.OnTeardown.PublishAsync(count).ConfigureAwait(false),
            };
        }

        public void AddExecuteResult(ExecuteResult[] results)
        {
            lock (executeResult)
            {
                executeResult.AddRange(results);
            }
        }
    }

    public static class WorkerProgressNotifier
    {
        public static WorkerProgress OnConnected = new WorkerProgress();
        public static WorkerProgress OnTeardown = new WorkerProgress();

        public class WorkerProgress
        {
            private readonly System.Threading.Channels.Channel<int> _channel;

            public Action<int>? OnPublished { get; set; }

            public WorkerProgress()
            {
                // 1 writer : n reader
                _channel = System.Threading.Channels.Channel.CreateUnbounded<int>(new System.Threading.Channels.UnboundedChannelOptions
                {
                    SingleWriter = true,
                });
            }

            public async ValueTask PublishAsync(int count)
            {
                await _channel.Writer.WriteAsync(count).ConfigureAwait(false);
                OnPublished?.Invoke(count);
            }
        }
    }
}