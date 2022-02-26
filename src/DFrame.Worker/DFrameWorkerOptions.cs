using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;

namespace DFrame
{
    public class DFrameWorkerOptions
    {
        public string ControllerAddress { get; set; } = default!;
        public TimeSpan ConnectTimeout { get; set; } = TimeSpan.FromMinutes(1);
        public TimeSpan ReconnectTime { get; set; } = TimeSpan.FromSeconds(5);
#if !UNITY_2020_1_OR_NEWER
        public SocketsHttpHandlerOptions SocketsHttpHandlerOptions { get; set; } = new SocketsHttpHandlerOptions();
#else
        public Grpc.Core.ChannelCredentials GrpcChannelCredentials { get; set; } = Grpc.Core.ChannelCredentials.Insecure;
        public IEnumerable<Grpc.Core.ChannelOption> GrpcChannelOptions { get; set; } = Array.Empty<Grpc.Core.ChannelOption>();
#endif
        public Assembly[] WorkloadAssemblies { get; set; } = AppDomain.CurrentDomain.GetAssemblies();
        public Dictionary<string, string> Metadata { get; set; } = new Dictionary<string, string>();
        public bool IncludesDefaultHttpWorkload { get; set; } = false;
        public int VirtualProcess { get; set; } = 1;
        public int MinBatchRate { get; set; } = 500;
        public int MaxBatchRate { get; set; } = 1000;
        public int BatchRate
        {
            set
            {
                MinBatchRate = MaxBatchRate = value;
            }
        }

        public DFrameWorkerOptions()
        {
        }

        public DFrameWorkerOptions(string controllerAddress)
        {
            this.ControllerAddress = controllerAddress;
        }
    }

    public class SocketsHttpHandlerOptions
    {
        public TimeSpan PooledConnectionIdleTimeout { get; set; } = Timeout.InfiniteTimeSpan;
        public TimeSpan PooledConnectionLifetime { get; set; } = Timeout.InfiniteTimeSpan;
        public TimeSpan KeepAlivePingDelay { get; set; } = TimeSpan.FromSeconds(60);
        public TimeSpan KeepAlivePingTimeout { get; set; } = TimeSpan.FromSeconds(30);
    }
}