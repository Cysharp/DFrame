using System;
using System.Threading.Tasks;
using MagicOnion;
using MagicOnion.Server;
using MagicOnion.Server.Hubs;
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
    public interface IEchoHub : IStreamingHub<IEchoHub, IEchoHubReceiver>
    {
        Task<MessageResponse> EchoAsync(string message);
    }

    [MessagePackObject]
    public struct MessageResponse
    {
        [Key(0)]
        public string Message { get; set; }
    }
}
