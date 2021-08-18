using DFrame;
using DFrame.Ecs;
using EchoMagicOnion.Shared;
using Grpc.Net.Client;
using MagicOnion.Client;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using ZLogger;

namespace ConsoleAppEcs
{
    class Program
    {
        static async Task Main(string[] args)
        {
            //GrpcEnvironment.SetLogger(new Grpc.Core.Logging.ConsoleLogger());

            var host = "0.0.0.0";
            var port = int.Parse(Environment.GetEnvironmentVariable("DFRAME_MASTER_CONNECT_TO_PORT") ?? "12345");
            var workerConnectToHost = Environment.GetEnvironmentVariable("DFRAME_MASTER_CONNECT_TO_HOST") ?? $"dframe-master.dframe.svc.cluster.local";
            // TODO:test args.
            if (args.Length == 0)
            {
                // master
                //args = new[] { "help", "rampup" };
                args = "batch -processCount 1 -workerPerProcess 10 -executePerWorker 10 -workerName SampleWorker".Split(' ');

                //args = "rampup -processCount 5 -maxWorkerPerProcess 12 -workerSpawnCount 4 -workerSpawnSecond 5 -workerName SampleHttpWorker".Split(' ');

                // args = "request -processCount 5 -workerPerProcess 10 -executePerWorker 10 -workerName SampleHttpWorker".Split(' ');
                //args = "-processCount 1 -workerPerProcess 64     -executePerWorker 10000 -workerName SampleHttpWorker".Split(' ');
                //args = "-processCount 1 -workerPerProcess 20 -executePerWorker 10000 -workerName SampleUnaryWorker".Split(' ');

                //args = "-processCount 1 -workerPerProcess 10 -executePerWorker 1000 -workerName SampleHttpWorker".Split(' ');
                //args = "-processCount 1 -workerPerProcess 10 -executePerWorker 10000 -workerName SampleHttpWorker".Split(' ');
                //args = "-processCount 10 -workerPerProcess 10 -executePerWorker 1000 -workerName SampleHttpWorker".Split(' ');
                //args = "-processCount 1 -workerPerProcess 10 -executePerWorker 1000 -workerName SampleUnaryWorker".Split(' ');
                //args = "-processCount 1 -workerPerProcess 10 -executePerWorker 10000 -workerName SampleUnaryWorker".Split(' ');
                //args = "-processCount 10 -workerPerProcess 10 -executePerWorker 1000 -workerName SampleUnaryWorker".Split(' ');
                //args = "-processCount 1 -workerPerProcess 10 -executePerWorker 1000 -workerName SampleStreamWorker".Split(' ');
                //args = "-processCount 1 -workerPerProcess 10 -executePerWorker 10000 -workerName SampleStreamWorker".Split(' ');
                //args = "-processCount 10 -workerPerProcess 10 -executePerWorker 1000 -workerName SampleStreamWorker".Split(' ');
            }
            else if (args.Contains("--worker-flag"))
            {
                // worker
                // connect to
                host = workerConnectToHost;
            }

            Console.WriteLine($"args {string.Join(", ", args)}, host {host}");
            await Host.CreateDefaultBuilder(args)
                .ConfigureLogging(logging =>
                {
                    logging.ClearProviders();
                    logging.SetMinimumLevel(LogLevel.Trace);
                    logging.AddZLoggerConsole(options =>
                    {
                        options.EnableStructuredLogging = false;
                    });
                })
                .RunDFrameLoadTestingAsync(args, new DFrameOptions(host, port, workerConnectToHost, port, new EcsScalingProvider())
                {
                });
        }
    }

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously

    public class SampleWorker : Worker
    {
        IDistributedQueue<byte> queue;

        public override async Task SetupAsync(WorkerContext context)
        {
            Console.WriteLine("Create DistributedQueue.");
            queue = context.CreateDistributedQueue<byte>("foo");
        }

        public override async Task ExecuteAsync(WorkerContext context)
        {
            var randI = (byte)new Random().Next(1, 100);
            Console.WriteLine($"Enqueue from {Environment.MachineName} {context.WorkerId}: {randI}");

            await queue.EnqueueAsync(randI);
        }

        public override async Task TeardownAsync(WorkerContext context)
        {
            while (true)
            {
                var v = await queue.TryDequeueAsync();
                if (v.HasValue)
                {
                    Console.WriteLine($"Dequeue all from {Environment.MachineName} {context.WorkerId}: {v.Value}");
                }
                else
                {
                    return;
                }
            }
        }
    }

    public class SampleHttpWorker : Worker
    {
        private static HttpClient httpClient;

        private string url;
        private CancellationTokenSource cts;

        static SampleHttpWorker()
        {
            var handler = new HttpClientHandler
            {
                MaxConnectionsPerServer = 100,
            };
            httpClient = new HttpClient(handler);
            httpClient.DefaultRequestHeaders.Add("ContentType", "application/json");
            Console.WriteLine($"MaxConnectionsPerServer: {handler.MaxConnectionsPerServer}");
        }

        public override async Task SetupAsync(WorkerContext context)
        {
            url = Environment.GetEnvironmentVariable("BENCH_SERVER_HOST");
            cts = new CancellationTokenSource(TimeSpan.FromMinutes(10));
        }

        public override async Task ExecuteAsync(WorkerContext context)
        {
            await httpClient.GetAsync(url, cts.Token);
            //await httpClient.GetAsync(_url);
        }

        public override async Task TeardownAsync(WorkerContext context)
        {
        }
    }

    public class SampleUnaryWorker : Worker
    {
        private GrpcChannel _channel;
        private IEchoService _client;

        public override async Task SetupAsync(WorkerContext context)
        {
            var url = Environment.GetEnvironmentVariable("BENCH_SERVER_HOST");
            _channel = GrpcChannel.ForAddress(url + ":12346");
            _client = MagicOnionClient.Create<IEchoService>(_channel);
        }
        public override async Task ExecuteAsync(WorkerContext context)
        {
            await _client.Echo(context.WorkerId);
        }

        public override async Task TeardownAsync(WorkerContext context)
        {
            await _channel.ShutdownAsync().ConfigureAwait(false);
        }
    }

    public class SampleStreamWorker : Worker
    {
        private GrpcChannel _channel;
        private IEchoHub _client;

        public override async Task SetupAsync(WorkerContext context)
        {
            var url = Environment.GetEnvironmentVariable("BENCH_SERVER_HOST");
            _channel = GrpcChannel.ForAddress(url + ":12346");
            var receiver = new EchoReceiver(_channel);
            _client = await StreamingHubClient.ConnectAsync<IEchoHub, IEchoHubReceiver>(_channel, receiver);
            receiver.Client = _client;
        }
        public override async Task ExecuteAsync(WorkerContext context)
        {
            await _client.EchoAsync(context.WorkerId);
            //await _client.EchoBroadcastAsync(context.WorkerId);
        }

        public override async Task TeardownAsync(WorkerContext context)
        {
            await _client.DisposeAsync();
            await _channel.ShutdownAsync().ConfigureAwait(false);
        }
    }
}