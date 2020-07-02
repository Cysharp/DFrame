using DFrame.Core;
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
            args = "-workerPerHost 10 -executePerWorker 10 -scenarioName ConsoleApp.SampleWorker".Split(' ');

            await Host.CreateDefaultBuilder(args).RunDFrameAsync(args, new DFrameOptions(12345..12348, new InProcessScaler()
            {
            }));
        }
    }

    public class SampleWorker : WorkerBase
    {
        public override Task SetupAsync()
        {
            return base.SetupAsync();
        }

        public override Task ExecuteAsync()
        {
            Console.WriteLine("Hello");
            return Task.CompletedTask;
        }

        public override Task TeardownAsync()
        {
            return base.TeardownAsync();
        }
    }
}