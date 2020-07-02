using Grpc.Core;
using Microsoft.Extensions.Hosting;
using System;
using System.Threading.Tasks;

namespace DFrame.Core
{
    public interface IWorkerScaler : IDisposable
    {
        Task<Channel> StartWorkerHostAsync(WorkerScalerOptions options, string?[] args, int port);
    }

    public class WorkerScalerOptions
    {
        public Func<string?[], IHostBuilder> HostBuilderFactory { get; set; }

        public WorkerScalerOptions()
        {
            HostBuilderFactory = args => Host.CreateDefaultBuilder(args);
        }
    }
}