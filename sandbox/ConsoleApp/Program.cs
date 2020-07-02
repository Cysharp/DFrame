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
            args = "-workerPerHost 3 -executePerWorker 3 -scenarioName ConsoleApp.SampleWorker".Split(' ');

            await Host.CreateDefaultBuilder(args).RunDFrameAsync(args, new DFrameOptions(12344, 12345..12345, new InProcessScaler()
            {
            }));
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
}