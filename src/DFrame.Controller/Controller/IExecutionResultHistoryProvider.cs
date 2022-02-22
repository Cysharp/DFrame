using System.Runtime.Serialization;

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

    // Serializable
    [DataContract]
    public class ExecutionSummary
    {
        // inits
        [DataMember]
        public string Workload { get; set; } = default!;
        [DataMember]
        public ExecutionId ExecutionId { get; set; }
        [DataMember]
        public int WorkerCount { get; set; }
        [DataMember]
        public int WorkloadCount { get; set; }
        [DataMember]
        public int Concurrency { get; set; }
        [DataMember]
        public KeyValuePair<string, string?>[] Parameters { get; set; } = default!;
        [DataMember]
        public DateTime StartTime { get; set; }

        // modifiable
        [DataMember]
        public TimeSpan? RunningTime { get; set; }
        [DataMember]
        public long TotalRequest { get; set; } // set in init and after completed
        [DataMember]
        public long? SucceedSum { get; set; }
        [DataMember]
        public long? ErrorSum { get; set; }
        [DataMember]
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