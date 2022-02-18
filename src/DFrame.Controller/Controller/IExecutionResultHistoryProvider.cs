namespace DFrame.Controller
{
    public interface IExecutionResultHistoryProvider
    {
        public event Action? NotifyCountChanged;
        int GetCount();
        IReadOnlyList<ExecutionSummary> GetList();
        (ExecutionSummary Summary, SummarizedExecutionResult[] Results)? GetResult(ExecutionId executionId);
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
        readonly Dictionary<ExecutionId, (ExecutionSummary, SummarizedExecutionResult[])> resultsLookup = new();

        public event Action? NotifyCountChanged;

        public int GetCount()
        {
            lock (gate)
            {
                return summaries.Count;
            }
        }

        public IReadOnlyList<ExecutionSummary> GetList()
        {
            lock (gate)
            {
                return summaries.ToArray();
            }
        }

        public (ExecutionSummary, SummarizedExecutionResult[])? GetResult(ExecutionId executionId)
        {
            lock (gate)
            {
                return resultsLookup.TryGetValue(executionId, out var result) ? result : null;
            }
        }

        public void AddNewResult(ExecutionSummary summary, SummarizedExecutionResult[] results)
        {
            lock (gate)
            {
                summaries.Add(summary);
                resultsLookup.Add(summary.ExecutionId, (summary, results));
                NotifyCountChanged?.Invoke();
            }
        }
    }
}