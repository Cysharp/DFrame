using System.Threading;

namespace DFrame
{
    public class WorkloadContext
    {
        public ExecutionId ExecutionId { get; }
        public WorkloadId WorkloadId { get; }
        public int WorkloadCount { get; }
        public int WorkloadIndex { get; }
        public int Concurrency { get; }
        public long TotalRequestCount { get; }
        public CancellationToken CancellationToken { get; }

        public WorkloadContext(ExecutionId executionId, int count, int index, int concurrency, long totalRequestCount, CancellationToken cancellationToken)
        {
            this.ExecutionId = executionId;
            this.WorkloadId = WorkloadId.New();
            this.WorkloadCount = count;
            this.WorkloadIndex = index;
            this.Concurrency = concurrency;
            this.TotalRequestCount = totalRequestCount;
            this.CancellationToken = cancellationToken;
        }
    }
}