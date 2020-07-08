using ConsoleAppFramework;
using DFrame.Core.Collections;
using DFrame.Core.Internal;
using Grpc.Core;
using MagicOnion.Client;
using MagicOnion.Hosting;
using MagicOnion.Server;
using MessagePack;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Linq;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Threading.Tasks;

namespace DFrame.Core
{
    public static class DFrameAppHostBuilderExtensions
    {
        public static async Task RunDFrameAsync(this IHostBuilder hostBuilder, string[] args, DFrameOptions options)
        {
            if (args.Length != 0 && args.Contains("--worker-flag"))
            {
                await hostBuilder
                    .ConfigureServices(x => x.AddSingleton(options))
                    .RunConsoleAppFrameworkAsync<DFrameWorkerApp>(args);
            }
            else
            {
                await hostBuilder
                    .ConfigureServices(x => x.AddSingleton(options))
                    .RunConsoleAppFrameworkAsync<DFrameApp>(args);
            }
        }
    }

    internal class DFrameApp : ConsoleAppBase
    {
        DFrameOptions options;
        IHost? masterHost;

        public DFrameApp(DFrameOptions options)
        {
            this.options = options;
        }

        public async Task Main(int nodeCount, int workerPerNode, int executePerWorker, string scenarioName)
        {
            using (masterHost = StartMasterHost())
            await using (options.ScalingProvider)
            {
                Console.WriteLine($"{nameof(DFrameApp)} Get reporter from DI");
                var reporter = masterHost.Services.GetRequiredService<Reporter>();
                reporter.Reset(nodeCount);

                Console.WriteLine($"{nameof(DFrameApp)} Start Worker");
                await options.ScalingProvider.StartWorkerAsync(options, nodeCount, Context.CancellationToken).WithCancellation(Context.CancellationToken);

                Console.WriteLine($"{nameof(DFrameApp)} reporter wait connect");
                await reporter.OnConnected.Waiter.WithCancellation(Context.CancellationToken);

                var broadcaster = reporter.Broadcaster;

                Console.WriteLine($"{nameof(DFrameApp)} broadcaster create co-worker");
                broadcaster.CreateCoWorker(workerPerNode, scenarioName);
                await reporter.OnCreateCoWorker.Waiter.WithCancellation(Context.CancellationToken);

                Console.WriteLine($"{nameof(DFrameApp)} broadcaster setup");
                broadcaster.Setup();
                await reporter.OnSetup.Waiter.WithCancellation(Context.CancellationToken);

                Console.WriteLine($"{nameof(DFrameApp)} broadcaster execute worker");
                broadcaster.Execute(executePerWorker);
                await reporter.OnExecute.Waiter.WithCancellation(Context.CancellationToken);

                Console.WriteLine($"{nameof(DFrameApp)} broadcaster execute teardown");
                broadcaster.Teardown();
                await reporter.OnTeardown.Waiter.WithCancellation(Context.CancellationToken);

                Console.WriteLine($"{nameof(DFrameApp)} broadcaster shutdown");
                broadcaster.Shutdown();
            }
        }

        IHost StartMasterHost()
        {
            var host = options.HostBuilderFactory(Context.Arguments)
                .UseMagicOnion(targetTypes: new Type[]
                {
                    typeof(MasterHub),
                    typeof(DistributedQueueService),
                }, options: new MagicOnionOptions
                {
                    IsReturnExceptionStackTraceInErrorDetail = true,
                    SerializerOptions = MessagePackSerializer.Typeless.DefaultOptions // use Typeless.
                }, ports: new ServerPort(options.Host, options.Port, ServerCredentials.Insecure))
                .ConfigureServices(x =>
                {
                    x.AddSingleton<Reporter>();
                })
                .Build();

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
        DFrameOptions options;

        public DFrameWorkerApp(DFrameOptions options)
        {
            this.options = options;
        }

        public async Task Main()
        {
            Console.WriteLine($"{nameof(DFrameWorkerApp)} create channel");
            var channel = new Channel(options.Host, options.Port, ChannelCredentials.Insecure);
            Console.WriteLine($"{nameof(DFrameWorkerApp)} create receiver");
            var receiver = new WorkerReceiver(channel);
            Console.WriteLine($"{nameof(DFrameWorkerApp)} create client");
            var client = StreamingHubClient.Connect<IMasterHub, IWorkerReceiver>(channel, receiver);
            receiver.Client = client;

            Console.WriteLine($"{nameof(DFrameWorkerApp)} client connect complete async");
            await client.ConnectCompleteAsync();
            Console.WriteLine($"{nameof(DFrameWorkerApp)} receiver wait shutdown");
            await receiver.WaitShutdown.WithCancellation(Context.CancellationToken);
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


    }
}