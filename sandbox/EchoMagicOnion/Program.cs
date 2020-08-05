using System;
using System.Threading.Tasks;
using EchoMagicOnion.Shared;
using MagicOnion;
using MagicOnion.Hosting;
using MagicOnion.Server;
using MagicOnion.Server.Hubs;
using MessagePack;
using Microsoft.Extensions.Hosting;

namespace EchoMagicOnion
{
    class Program
    {
        static async Task Main(string[] args)
        {
            //GrpcEnvironment.SetLogger(new Grpc.Core.Logging.ConsoleLogger());

            await MagicOnionHost.CreateDefaultBuilder()
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
        public Task<MessageResponse> EchoAsync(string message)
        {
            var response = new MessageResponse { Message = message };
            return Task.FromResult(response);
        }
    }
}
