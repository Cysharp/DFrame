using System;
using System.Threading.Tasks;
using Grpc.Core;
using MagicOnion;
using MessagePack;

namespace EchoMagicOnion.Shared
{
    // unary
    public interface IEchoService : IService<IEchoService>
    {
        UnaryResult<Nil> Echo(string message);
    }

    // streaming hub
    public interface IEchoHubReceiver
    {
        void OnSend(MessageResponse message);
    }
    public class EchoReceiver : IEchoHubReceiver
    {
        readonly Channel channel;

        public EchoReceiver(Channel channel)
        {
            this.channel = channel;
        }

        public IEchoHub Client { get; set; } = default!;

        public async void OnSend(MessageResponse message)
        {
            //Console.WriteLine("Reciever:" + message.Message);
            await Client.EchoAsync(message.Message);
        }
    }

    public interface IEchoHub : IStreamingHub<IEchoHub, IEchoHubReceiver>
    {
        Task<MessageResponse> EchoAsync(string message);
        Task<MessageResponse> EchoBroadcastAsync(string message);
    }

    [MessagePackObject]
    public struct MessageResponse
    {
        [Key(0)]
        public string Message { get; set; }
    }
}
