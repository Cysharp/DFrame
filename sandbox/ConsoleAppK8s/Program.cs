using DFrame;
using DFrame.Core;
using DFrame.Core.Collections;
using DFrame.KubernetesWorker;
using Microsoft.Extensions.Hosting;
using System;
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

            Console.WriteLine($"args {string.Join(", ", args)}, host {host}");
            await Host.CreateDefaultBuilder(args).RunDFrameAsync(args, new DFrameOptions(host, 12345, new KubernetesScalingProvider())
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
            queue = context.CreateDistributedQueue<byte>();
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
                var (ok, value) = await queue.TryDequeueAsync();
                if (!ok) return;

                Console.WriteLine($"Dequeue all from {Environment.MachineName} {context.WorkerId}: {value}");
            }
        }
    }
}