using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;

namespace DFrame.Core
{
    public class DFrameOptions
    {
        public string Host { get; }
        public int Port { get; }
        public IScalingProvider ScalingProvider { get; }
        public Func<string?[], IHostBuilder> HostBuilderFactory { get; set; }

        public Action<ExecuteResult[], DFrameOptions>? OnExecuteResult { get; set; }

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