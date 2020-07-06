using Microsoft.Extensions.Hosting;
using System;

namespace DFrame.Core
{
    public class DFrameOptions
    {
        public string Host { get; }
        public int Port { get; }
        public IScalingProvider ScalingProvider { get; }
        public Func<string?[], IHostBuilder> HostBuilderFactory { get; set; }

        public DFrameOptions(string host, int port, IScalingProvider scalingProvider)
        {
            Host = host;
            Port = port;
            ScalingProvider = scalingProvider;
            // TODO:configuration?
            HostBuilderFactory = args => Microsoft.Extensions.Hosting.Host.CreateDefaultBuilder(args);
        }
    }
}