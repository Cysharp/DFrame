using Grpc.Core;
using Grpc.Net.Client;
using MagicOnion;
using MagicOnion.Client;
using MessagePack;

namespace DFrame;

public class WorkloadContext
{
    // TODO:remove options?
    readonly GrpcChannel masterChannel;
    readonly MessagePackSerializerOptions serializerOptions;

    public WorkloadId WorkloadId { get; }

    public WorkloadContext(GrpcChannel masterChannel, DFrameOptions options)
    {
        this.masterChannel = masterChannel;
        this.WorkloadId = WorkloadId.NewWorkloadId();
        this.serializerOptions = options.SerializerOptions;
    }
}
