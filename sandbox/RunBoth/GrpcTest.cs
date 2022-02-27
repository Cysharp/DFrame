#pragma warning disable CS1998

using DFrame;
using Grpc.Net.Client;
using MinimumGrpc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RunBoth
{
    public class GrpcTest : Workload
    {
        GrpcChannel? channel;
        Greeter.GreeterClient? client;

        public override async Task SetupAsync(WorkloadContext context)
        {
            channel = GrpcChannel.ForAddress("http://localhost:5027");
            client = new Greeter.GreeterClient(channel);
        }

        public override async Task ExecuteAsync(WorkloadContext context)
        {
            await client!.SayHelloAsync(new HelloRequest(), cancellationToken: context.CancellationToken);
        }

        public override async Task TeardownAsync(WorkloadContext context)
        {
            if (channel != null)
            {
                await channel.ShutdownAsync();
                channel.Dispose();
            }
        }
    }

    public class GrpcShareChannel : Workload
    {
        static GrpcChannel channel = default!;
        Greeter.GreeterClient client = default!;

        static GrpcShareChannel()
        {
            var handler = new SocketsHttpHandler()
            {
                //EnableMultipleHttp2Connections = true
                EnableMultipleHttp2Connections = false
            };
            channel = GrpcChannel.ForAddress("http://localhost:5027", new GrpcChannelOptions
            {
                HttpHandler = handler
            });
        }

        public override async Task SetupAsync(WorkloadContext context)
        {
            client = new Greeter.GreeterClient(channel);
        }

        public override async Task ExecuteAsync(WorkloadContext context)
        {
            await client.SayHelloAsync(new HelloRequest());
        }

        public override async Task TeardownAsync(WorkloadContext context)
        {
        }
    }
}
