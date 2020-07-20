using DFrame;
using DFrame.Collections;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Hosting;
using System;
using System.Threading.Tasks;
using System.Net.Http;

namespace ConsoleApp
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var host = "localhost";
            // TODO:test args.
            if (args.Length == 0)
            {
                // master
                args = "-nodeCount 3 -workerPerNode 3 -executePerWorker 3 -scenarioName ConsoleAppK8s.SampleWorker".Split(' ');
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

            await Host.CreateDefaultBuilder(args)
                .ConfigureLogging(x =>
                {
                    x.SetMinimumLevel(LogLevel.Trace);
                })
                .RunDFrameLoadTestingAsync(args, new DFrameOptions(host + ":12345", host + ":12345", new InProcessScalingProvider())
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
        private readonly string _url = "http://77948c50-apiserver-apiserv-98d9-538745285.ap-northeast-1.elb.amazonaws.com/healthz";
        //private readonly string _url = "http://77948c50-apiserver-apiserv-98d9-538745285.ap-northeast-1.elb.amazonaws.com/api/weatherforecast";
        private static readonly HttpClient _httpClient;

        static SampleHttpWorker()
        {
            var handler = new HttpClientHandler
            {
                MaxConnectionsPerServer = 2,
            };
            _httpClient = new HttpClient(handler);
        }
        public override async Task SetupAsync(WorkerContext context)
        {
            //httpClient = new HttpClient();
            //httpClient.DefaultRequestHeaders.Add("ContentType", "application/json");
        }

        public override async Task ExecuteAsync(WorkerContext context)
        {
            await _httpClient.GetAsync(_url, HttpCompletionOption.ResponseHeadersRead);
        }

        public override async Task TeardownAsync(WorkerContext context)
        {
        }
    }
}