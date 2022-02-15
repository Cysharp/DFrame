namespace DFrame.Controller
{
    public interface IExecutionResultHistoryProvider
    {
        int Count { get; }
        IReadOnlyList<ExecutionSummary> GetList();
        SummarizedExecutionResult[] GetResult(ExecutionId executionId);
        void AddNewResult(ExecutionSummary summary, SummarizedExecutionResult[] results);
    }

    public class ExecutionSummary
    {
        // inits
        public string Workload { get; init; } = default!;
        public ExecutionId ExecutionId { get; init; }
        public int WorkerCount { get; init; }
        public int WorkloadCount { get; init; }
        public int Concurrency { get; init; }
        public int TotalRequest { get; init; }
        public (string name, string value)[] Parameters { get; init; } = default!;
        public DateTime StartTime { get; init; }

        // modifiable
        public TimeSpan? RunningTime { get; set; }
        public int? SucceedSum { get; set; }
        public int? ErrorSum { get; set; }
        public double? RpsSum { get; set; }
    }

    public class InMemoryExecutionResultHistoryProvider : IExecutionResultHistoryProvider
    {
        readonly object gate = new object();

        readonly List<ExecutionSummary> summaries = new List<ExecutionSummary>();
        readonly Dictionary<ExecutionId, SummarizedExecutionResult[]> resultsLookup = new Dictionary<ExecutionId, SummarizedExecutionResult[]>();

        public int Count
        {
            get
            {
                lock (gate)
                {
                    return summaries.Count;
                }
            }
        }

        public IReadOnlyList<ExecutionSummary> GetList()
        {
            lock (gate)
            {
                return summaries.ToArray();
            }
        }

        public SummarizedExecutionResult[] GetResult(ExecutionId executionId)
        {
            lock (gate)
            {
                return resultsLookup.TryGetValue(executionId, out var result) ? result : Array.Empty<SummarizedExecutionResult>();
            }
        }

        public void AddNewResult(ExecutionSummary summary, SummarizedExecutionResult[] results)
        {
            lock (gate)
            {
                summaries.Add(summary);
                resultsLookup.Add(summary.ExecutionId, results);
            }
        }
    }
}