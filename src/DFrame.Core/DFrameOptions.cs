using Microsoft.Extensions.Hosting;
using System;

namespace DFrame.Core
{
    public class DFrameOptions
    {
        public int MasterPort { get; }
        public Range WorkerPortRange { get; }
        public IWorkerScaler WorkerScaler { get; }
        public Func<string?[], IHostBuilder> HostBuilderFactory { get; set; }

        public DFrameOptions(int masterPort, Range workerPortRange, IWorkerScaler workerScaler)
        {
            MasterPort = masterPort;
            WorkerPortRange = workerPortRange;
            WorkerScaler = workerScaler;
            // TODO:configuration?
            HostBuilderFactory = args => Host.CreateDefaultBuilder(args);
        }
    }
}