using Grpc.Core;
using Grpc.Net.Client;
using MagicOnion;
using MagicOnion.Client;
using MessagePack;

namespace DFrame;

public class WorkloadContext
{
    readonly GrpcChannel masterChannel;
    readonly MessagePackSerializerOptions serializerOptions;

    public string WorkloadId { get; }

    public WorkloadContext(GrpcChannel masterChannel, DFrameOptions options)
    {
        this.masterChannel = masterChannel;
        this.WorkloadId = Guid.NewGuid().ToString();
        this.serializerOptions = options.SerializerOptions;
    }

    T CreateClient<T>(string key, string value)
        where T : IService<T>
    {
        return MagicOnionClient.Create<T>(
                masterChannel.CreateCallInvoker(),
                serializerOptions)
            .WithHeaders(new Metadata() { { key, value } });
    }
}
