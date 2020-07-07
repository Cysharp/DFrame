using DFrame;
using DFrame.Core;
using DFrame.Core.Collections;
using Microsoft.Extensions.Hosting;
using System;
using System.Threading.Tasks;

namespace ConsoleApp
{
    class Program
    {
        static async Task Main(string[] args)
        {
            // TODO:test args.
            if (args.Length == 0)
            {
                args = "-nodeCount 3 -workerPerNode 3 -executePerWorker 3 -scenarioName ConsoleApp.SampleWorker".Split(' ');
            }

            await Host.CreateDefaultBuilder(args).RunDFrameAsync(args, new DFrameOptions("localhost", 12345, new OutOfProcessScalingProvider())
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
            Console.WriteLine($"Enqueue from {context.WorkerId}: {randI}");

            await queue.EnqueueAsync(randI);
        }

        public override async Task TeardownAsync(WorkerContext context)
        {
            while (true)
            {
                var (ok, value) = await queue.TryDequeueAsync();
                if (!ok) return;

                Console.WriteLine($"Dequeue all from {context.WorkerId}: {value}");
            }
        }
    }

    public class SampleKubernetesWorker : Worker
    {
        IDistributedQueue<byte> queue;

        public override async Task SetupAsync(WorkerContext context)
        {
            queue = context.CreateDistributedQueue<byte>();
        }

        public override async Task ExecuteAsync(WorkerContext context)
        {
            var randI = (byte)new Random().Next(1, 100);
            Console.WriteLine($"running on {Environment.MachineName} {context.WorkerId}: {randI}");

            await queue.EnqueueAsync(randI);
        }

        public override async Task TeardownAsync(WorkerContext context)
        {
            while (true)
            {
                var (ok, value) = await queue.TryDequeueAsync();
                if (!ok) return;

                Console.WriteLine($"Dequeue all from {context.WorkerId}: {value}");
            }
        }
    }
}