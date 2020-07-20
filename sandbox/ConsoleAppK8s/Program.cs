using DFrame;
using DFrame.Collections;
using DFrame.KubernetesWorker;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace ConsoleAppK8s
{
    class Program
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine($"dframe begin. {nameof(KubernetesScalingProvider)}");
            var host = "localhost";
            // TODO:test args.
            if (args.Length == 0)
            {
                // master
                //args = "-nodeCount 3 -workerPerNode 3 -executePerWorker 3 -scenarioName ConsoleAppK8s.SampleWorker".Split(' ');
                //args = "-nodeCount 1 -workerPerNode 10 -executePerWorker 1000 -scenarioName ConsoleAppK8s.SampleHttpWorker".Split(' ');
                args = "-nodeCount 1 -workerPerNode 10 -executePerWorker 10000 -scenarioName ConsoleAppK8s.SampleHttpWorker".Split(' ');
                // listen on
                host = "0.0.0.0";
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

            Console.WriteLine($"args {string.Join(", ", args)}, host {host}");
            await Host.CreateDefaultBuilder(args)
                .ConfigureLogging(x =>
                {
                    x.SetMinimumLevel(LogLevel.Trace);
                })
                .RunDFrameLoadTestingAsync(args, new DFrameOptions(host + ":12345", host + ":12345", new KubernetesScalingProvider())
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

        private readonly string _url = "http://77948c50-apiserver-apiserv-98d9-538745285.ap-northeast-1.elb.amazonaws.com/healthz";
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
            //await httpClient.GetAsync(_url);
        }

        public override async Task TeardownAsync(WorkerContext context)
        {
        }
    }
}