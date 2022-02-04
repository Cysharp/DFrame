using Grpc.Core;
using Grpc.Net.Client;
using MagicOnion;
using MagicOnion.Client;
using MessagePack;

namespace DFrame;

public class WorkloadContext
{
    public WorkloadId WorkloadId { get; }
    public CancellationToken CancellationToken { get; }

    public WorkloadContext(CancellationToken cancellationToken)
    {
        this.WorkloadId = WorkloadId.NewWorkloadId();
        this.CancellationToken = cancellationToken;
    }
}
