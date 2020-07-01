using DFrame.Core;
using System;
using System.Threading.Tasks;

namespace ConsoleApp
{
    class Program
    {
        static async Task Main(string[] args)
        {
            await DFrameApp.RunAsync(args);
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