using DFrame;
using DFrame.Collections;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Hosting;
using System;
using System.Threading.Tasks;
using System.Net.Http;
using System.Threading;
using ZLogger;
using Grpc.Core;
using EchoMagicOnion.Shared;
using MagicOnion.Client;
using System.Linq;
using Grpc.Net.Client;

namespace ConsoleApp
{
    class Program
    {
        static async Task Main(string[] args)
        {
            // GrpcEnvironment.SetLogger(new Grpc.Core.Logging.ConsoleLogger());

            var host = "localhost";
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
                // listen on
                // host = "0.0.0.0";
            }
            else
            {
                // worker
                // connect to
                var envHost = Environment.GetEnvironmentVariable("DFRAME_MASTER_HOST");
                host = args.Length >= 2
                    ? args[1]
                    : !string.IsNullOrEmpty(envHost)
                        ? envHost
                        : "localhost";
            }

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
                //.RunDFrameAsync(args, new DFrameOptions(host, 12345)
                .RunDFrameLoadTestingAsync(args, new DFrameOptions(host, 12345, host, 12345, new InProcessScalingProvider())
                {
                });
        }
    }

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously

    public class SampleWorkload : Workload
    {
        IDistributedQueue<int> queue;

        public override async Task SetupAsync(WorkloadContext context)
        {
            queue = context.CreateDistributedQueue<int>("sampleworkload-testq");
        }

        public override async Task ExecuteAsync(WorkloadContext context)
        {
            var randI = (int)new Random().Next(1, 3999);
            //Console.WriteLine($"Enqueue from {Environment.MachineName} {context.WorkerId}: {randI}");

            await queue.EnqueueAsync(randI);
        }

        public override async Task TeardownAsync(WorkloadContext context)
        {
            while (true)
            {
                var v = await queue.TryDequeueAsync();
                if (v.HasValue)
                {
                    //Console.WriteLine($"Dequeue all from {Environment.MachineName} {context.WorkerId}: {v.Value}");
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

        private readonly string _url = "http://localhost:5000";
        private CancellationTokenSource cts;

        static SampleHttpWorkload()
        {
            var handler = new HttpClientHandler
            {
                // MaxConnectionsPerServer = 100,
            };
            httpClient = new HttpClient(handler);
            httpClient.DefaultRequestHeaders.Add("ContentType", "application/json");

            // keepalive=false
            //httpClient.DefaultRequestHeaders.Add("Connection", "close");

            // Console.WriteLine($"MaxConnectionsPerServer: {handler.MaxConnectionsPerServer}");
        }

        public override async Task SetupAsync(WorkloadContext context)
        {
            cts = new CancellationTokenSource(TimeSpan.FromMinutes(10));
        }

        public override async Task ExecuteAsync(WorkloadContext context)
        {
            await httpClient.GetAsync(_url, cts.Token);
            // await r.Content.ReadAsByteArrayAsync();
        }

        public override async Task TeardownAsync(WorkloadContext context)
        {
        }
    }

    public class SampleUnaryWorkload : Workload
    {
        private GrpcChannel _channel;
        private IEchoService _client;

        private readonly string _host = "http://localhost";

        public override async Task SetupAsync(WorkloadContext context)
        {
            _channel = GrpcChannel.ForAddress(_host + ":12346", new GrpcChannelOptions
            {
                Credentials = ChannelCredentials.Insecure,
            });
            _client = MagicOnionClient.Create<IEchoService>(_channel);
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

        private readonly string _host = "http://localhost";

        public override async Task SetupAsync(WorkloadContext context)
        {
            _channel = GrpcChannel.ForAddress(_host + ":12346");
            var receiver = new EchoReceiver(_channel);
            _client = await StreamingHubClient.ConnectAsync<IEchoHub, IEchoHubReceiver>(_channel, receiver);
            receiver.Client = _client;
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