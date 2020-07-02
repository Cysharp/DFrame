using Grpc.Core;
using MagicOnion.Hosting;
using MagicOnion.Server;
using Microsoft.Extensions.Hosting;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace DFrame.Core
{
    public class InProcessScaler : IWorkerScaler
    {
        CancellationTokenSource cts = new CancellationTokenSource();
        List<IHost> hosts = new List<IHost>();

        public Task<Channel> StartWorkerHostAsync(WorkerScalerOptions options, string?[] args, int port)
        {
            var host = options.HostBuilderFactory(args)
                .UseMagicOnion(targetTypes: new[] { typeof(WorkerHub) }, options: new MagicOnionOptions
                {
                    IsReturnExceptionStackTraceInErrorDetail = true
                }, ports: new ServerPort("localhost", port, ServerCredentials.Insecure))
                .UseConsoleLifetime()
                .Build();
            hosts.Add(host);
            host.RunAsync(cts.Token);

            var channel = new Channel("localhost", port, ChannelCredentials.Insecure);
            return Task.FromResult(channel);
        }

        public void Dispose()
        {
            cts.Cancel();
            cts.Dispose();
            foreach (var item in hosts)
            {
                item.Dispose();
            }
        }
    }
}