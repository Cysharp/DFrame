using DFrame;
using Grpc.Net.Client;
using MagicOnion;
using MagicOnion.Client;
using MessagePack;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RunBoth
{
    public class MagicOnionUnary : Workload
    {
        GrpcChannel channel = default!;
        IEchoService client = default!;

        public override Task SetupAsync(WorkloadContext context)
        {
            channel = GrpcChannel.ForAddress("http://localhost:5059");
            client = MagicOnionClient.Create<IEchoService>(channel);
            return base.SetupAsync(context);
        }

        public override async Task ExecuteAsync(WorkloadContext context)
        {
            await client.Echo(null!);
        }

        public override async Task TeardownAsync(WorkloadContext context)
        {
            await channel.ShutdownAsync();
            channel.Dispose();
        }
    }

    public class MagicOnionStreamingHub : Workload, IEchoHubReceiver
    {
        GrpcChannel channel = default!;
        IEchoHub client = default!;

        public override async Task SetupAsync(WorkloadContext context)
        {
            channel = GrpcChannel.ForAddress("http://localhost:5059");
            client = await StreamingHubClient.ConnectAsync<IEchoHub, IEchoHubReceiver>(channel, this);
        }

        public override async Task ExecuteAsync(WorkloadContext context)
        {
            await client.EchoAsync(null!);
        }

        public override async Task TeardownAsync(WorkloadContext context)
        {
            await client.DisposeAsync();
            await channel.ShutdownAsync();
            channel.Dispose();
        }
    }


    // unary
    public interface IEchoService : IService<IEchoService>
    {
        UnaryResult<Nil> Echo(string message);
    }

    // streaming hub
    public interface IEchoHubReceiver
    {
    }

    public interface IEchoHub : IStreamingHub<IEchoHub, IEchoHubReceiver>
    {
        Task<Nil> EchoAsync(string message);
    }
}
