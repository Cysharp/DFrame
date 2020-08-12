using System;
using System.Threading.Tasks;
using EchoMagicOnion.Shared;
using MagicOnion;
using MagicOnion.Hosting;
using MagicOnion.Server;
using MagicOnion.Server.Hubs;
using MessagePack;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ZLogger;

namespace EchoMagicOnion
{
    class Program
    {
        static async Task Main(string[] args)
        {
            //GrpcEnvironment.SetLogger(new Grpc.Core.Logging.ConsoleLogger());

            await MagicOnionHost.CreateDefaultBuilder()
                .ConfigureLogging((context, logging) =>
                {
                    logging.ClearProviders();
                    logging.SetMinimumLevel(LogLevel.Trace);
                    logging.AddZLoggerConsole();
                })
                .UseMagicOnion()
                .RunConsoleAsync();
        }
    }

    public class EchoService : ServiceBase<IEchoService>, IEchoService
    {
        public UnaryResult<Nil> Echo(string message)
        {
            return UnaryResult(Nil.Default);
        }
    }

    public class EchoHub : StreamingHubBase<IEchoHub, IEchoHubReceiver>, IEchoHub
    {
        private IEchoHubReceiver _broadcaster;
        protected override async ValueTask OnConnecting()
        {
            var group = await Group.AddAsync("global-masterhub-group");
            _broadcaster = group.CreateBroadcaster<IEchoHubReceiver>();
        }

        public Task<MessageResponse> EchoAsync(string message)
        {
            var response = new MessageResponse { Message = message };

            return Task.FromResult(response);
        }

        public Task<MessageResponse> EchoBroadcastAsync(string message)
        {
            var response = new MessageResponse { Message = message };

            // broadcast to all client
            _broadcaster.OnSend(response);

            return Task.FromResult(response);
        }
    }
}
