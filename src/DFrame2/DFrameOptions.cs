using System.Reflection;

namespace DFrame
{
    public class DFrameOptions
    {
        public string ControllerAddress { get; set; } = default!;
        public TimeSpan ConnectTimeout { get; set; } = TimeSpan.FromMinutes(1);
        public TimeSpan ReconnectTime { get; set; } = TimeSpan.FromSeconds(5);
        public SocketsHttpHandlerOptions SocketsHttpHandlerOptions { get; set; } = new SocketsHttpHandlerOptions();
        public Assembly[] WorkloadAssemblies { get; set; } = AppDomain.CurrentDomain.GetAssemblies();
        public Dictionary<string, string> Metadata { get; set; } = new Dictionary<string, string>();

        public DFrameOptions()
        {
        }

        public DFrameOptions(string controllerAddress)
        {
            this.ControllerAddress = controllerAddress;
        }
    }

    public struct SocketsHttpHandlerOptions
    {
        public TimeSpan PooledConnectionIdleTimeout { get; set; } = Timeout.InfiniteTimeSpan;
        public TimeSpan PooledConnectionLifetime { get; set; } = Timeout.InfiniteTimeSpan;
        public TimeSpan KeepAlivePingDelay { get; set; } = TimeSpan.FromSeconds(60);
        public TimeSpan KeepAlivePingTimeout { get; set; } = TimeSpan.FromSeconds(30);
    }
}