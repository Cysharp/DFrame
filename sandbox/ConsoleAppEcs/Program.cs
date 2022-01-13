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
                args = "batch -workerCount 1 -workloadName SampleWorkload".Split(' ');
                //args = "request -workerCount 5 -workloadPerWorker 10 -executePerWorkload 10 -workloadName SampleUnaryWorker".Split(' ');
                //args = "request -workerCount 5 -workloadPerWorker 10 -executePerWorkload 10 -workloadName SampleStreamWorker".Split(' ');

                //args = "rampup -workerCount 5 -maxworkloadPerWorker 12 -workerSpawnCount 4 -workerSpawnSecond 5 -workloadName SampleHttpWorker".Split(' ');

                // args = "request -workerCount 5 -workloadPerWorker 10 -executePerWorkload 10 -workloadName SampleHttpWorker".Split(' ');
                //args = "-workerCount 1 -workloadPerWorker 64     -executePerWorkload 10000 -workloadName SampleHttpWorker".Split(' ');
                //args = "-workerCount 1 -workloadPerWorker 20 -executePerWorkload 10000 -workloadName SampleUnaryWorker".Split(' ');

                //args = "-workerCount 1 -workloadPerWorker 10 -executePerWorkload 1000 -workloadName SampleHttpWorker".Split(' ');
                //args = "-workerCount 1 -workloadPerWorker 10 -executePerWorkload 10000 -workloadName SampleHttpWorker".Split(' ');
                //args = "-workerCount 10 -workloadPerWorker 10 -executePerWorkload 1000 -workloadName SampleHttpWorker".Split(' ');
                //args = "-workerCount 1 -workloadPerWorker 10 -executePerWorkload 1000 -workloadName SampleUnaryWorker".Split(' ');
                //args = "-workerCount 1 -workloadPerWorker 10 -executePerWorkload 10000 -workloadName SampleUnaryWorker".Split(' ');
                //args = "-workerCount 10 -workloadPerWorker 10 -executePerWorkload 1000 -workloadName SampleUnaryWorker".Split(' ');
                //args = "-workerCount 1 -workloadPerWorker 10 -executePerWorkload 1000 -workloadName SampleStreamWorker".Split(' ');
                //args = "-workerCount 1 -workloadPerWorker 10 -executePerWorkload 10000 -workloadName SampleStreamWorker".Split(' ');
                //args = "-workerCount 10 -workloadPerWorker 10 -executePerWorkload 1000 -workloadName SampleStreamWorker".Split(' ');
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

    public class SampleWorkload : Workload
    {
        IDistributedQueue<byte> queue;

        public override async Task SetupAsync(WorkloadContext context)
        {
            Console.WriteLine("Create DistributedQueue.");
            queue = context.CreateDistributedQueue<byte>("foo");
        }

        public override async Task ExecuteAsync(WorkloadContext context)
        {
            var randI = (byte)new Random().Next(1, 100);
            Console.WriteLine($"Enqueue from {Environment.MachineName} {context.WorkloadId}: {randI}");

            await queue.EnqueueAsync(randI);
        }

        public override async Task TeardownAsync(WorkloadContext context)
        {
            while (true)
            {
                var v = await queue.TryDequeueAsync();
                if (v.HasValue)
                {
                    Console.WriteLine($"Dequeue all from {Environment.MachineName} {context.WorkloadId}: {v.Value}");
                }
                else
                {
                    return;
                }
            }
        }
    }

    public class SampleHttpWorkload : Workload
    {
        private static HttpClient httpClient;

        private string url;
        private CancellationTokenSource cts;

        static SampleHttpWorkload()
        {
            var handler = new HttpClientHandler
            {
                //MaxConnectionsPerServer = 100,
            };
            httpClient = new HttpClient(handler);
            
            httpClient.DefaultRequestHeaders.Add("ContentType", "application/json");
        }

        public override async Task SetupAsync(WorkloadContext context)
        {
            url = Environment.GetEnvironmentVariable("BENCH_HTTP_SERVER_HOST");
            cts = new CancellationTokenSource(TimeSpan.FromMinutes(10));

            Console.WriteLine($"connect to: {url} ({nameof(SampleHttpWorkload)})");
        }

        public override async Task ExecuteAsync(WorkloadContext context)
        {
            await httpClient.GetAsync(url, cts.Token);
            //await httpClient.GetAsync(_url);
        }

        public override async Task TeardownAsync(WorkloadContext context)
        {
        }
    }

    public class SampleUnaryWorkload : Workload
    {
        private GrpcChannel _channel;
        private IEchoService _client;

        public override async Task SetupAsync(WorkloadContext context)
        {
            var url = Environment.GetEnvironmentVariable("BENCH_GRPC_SERVER_HOST");
            _channel = GrpcChannel.ForAddress(url);
            _client = MagicOnionClient.Create<IEchoService>(_channel);

            Console.WriteLine($"connect to: {url} ({nameof(SampleUnaryWorkload)})");
        }
        public override async Task ExecuteAsync(WorkloadContext context)
        {
            await _client.Echo(context.WorkloadId);
        }

        public override async Task TeardownAsync(WorkloadContext context)
        {
            await _channel.ShutdownAsync().ConfigureAwait(false);
        }
    }

    public class SampleStreamWorkload : Workload
    {
        private GrpcChannel _channel;
        private IEchoHub _client;

        public override async Task SetupAsync(WorkloadContext context)
        {
            var url = Environment.GetEnvironmentVariable("BENCH_GRPC_SERVER_HOST");
            _channel = GrpcChannel.ForAddress(url);

            Console.WriteLine($"connect to: {url} ({nameof(SampleStreamWorkload)})");

            var receiver = new EchoReceiver(_channel);
            _client = await StreamingHubClient.ConnectAsync<IEchoHub, IEchoHubReceiver>(_channel, receiver);
            receiver.Client = _client;

            Console.WriteLine($"stream hub connected.");
        }
        public override async Task ExecuteAsync(WorkloadContext context)
        {
            await _client.EchoAsync(context.WorkloadId);
            //await _client.EchoBroadcastAsync(context.WorkerId);
        }

        public override async Task TeardownAsync(WorkloadContext context)
        {
            await _client.DisposeAsync();
            await _channel.ShutdownAsync().ConfigureAwait(false);
        }
    }
}