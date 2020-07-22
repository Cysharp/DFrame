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
                args = "-processCount 3 -workerPerProcess 3 -executePerWorker 3 -workerName SampleWorker".Split(' ');
                //args = "-nodeCount 1 -workerPerNode 10 -executePerWorker 100 -scenarioName ConsoleApp.SampleHttpWorker".Split(' ');
                //args = "-nodeCount 1 -workerPerNode 10 -executePerWorker 10000 -scenarioName ConsoleApp.SampleHttpWorker".Split(' ');
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
                    //logging.SetMinimumLevel(LogLevel.Trace);
                    logging.AddZLoggerConsole(options =>
                    {
                        options.EnableStructuredLogging = false;
                    });
                })
                .RunDFrameLoadTestingAsync(args, new DFrameOptions(host, 12345)
                //.RunDFrameLoadTestingAsync(args, new DFrameOptions(host + ":12345", host + ":12345", new InProcessScalingProvider())
                {

                });
        }
    }

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously

    public class SampleWorker : Worker
    {
        IDistributedQueue<int> queue;

        public override async Task SetupAsync(WorkerContext context)
        {
            queue = context.CreateDistributedQueue<int>("sampleworker-testq");
        }

        public override async Task ExecuteAsync(WorkerContext context)
        {
            var randI = (int)new Random().Next(1, 3999);
            //Console.WriteLine($"Enqueue from {Environment.MachineName} {context.WorkerId}: {randI}");

            await queue.EnqueueAsync(randI);
        }

        public override async Task TeardownAsync(WorkerContext context)
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

    public class SampleHttpWorker : Worker
    {
        private static HttpClient httpClient;

        private readonly string _url = "http://localhost:5000";
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
            cts = new CancellationTokenSource(TimeSpan.FromMinutes(10));
        }

        public override async Task ExecuteAsync(WorkerContext context)
        {
            await httpClient.GetAsync(_url, cts.Token);
        }

        public override async Task TeardownAsync(WorkerContext context)
        {
        }
    }
}