using System.Threading;

namespace DFrame
{
    public class WorkloadContext
    {
        public WorkloadId WorkloadId { get; }
        public int WorkloadCount { get; }
        public int WorkloadIndex { get; }
        public CancellationToken CancellationToken { get; }

        public WorkloadContext(int count, int index, CancellationToken cancellationToken)
        {
            this.WorkloadId = WorkloadId.New();
            this.WorkloadCount = count;
            this.WorkloadIndex = index;
            this.CancellationToken = cancellationToken;
        }
    }
}