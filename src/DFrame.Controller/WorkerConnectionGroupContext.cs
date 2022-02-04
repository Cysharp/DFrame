namespace DFrame.Controller;

// Note: this class is too complex, need refactoring.

// Singleton Global State.
public class WorkerConnectionGroupContext : INotifyStateChanged
{
    static readonly IReadOnlyDictionary<string, string> EmptyMetadata = new Dictionary<string, string>();

    // Notify.
    public event Action? StateChanged;

    // lock share with RunningState
    internal readonly object ConnectionLock = new object();

    readonly Dictionary<WorkerId, Dictionary<string, string>?> connections = new();
    WorkloadInfo[] workloadInfos = Array.Empty<WorkloadInfo>();

    public WorkloadInfo[] WorkloadInfos => workloadInfos;

    public int CurrentConnectingCount { get; private set; }
    public bool IsRunning => RunningState != null;
    public RunningState? RunningState { get; private set; }
    public ExecutionId? CurrentExecutionId { get; private set; }

    Dictionary<WorkerId, int>? latestResults; // index of latestResultsSorted
    SummarizedExecutionResult[]? latestResultsSorted; // for performance reason, store stored array.

    public SummarizedExecutionResult[] LatestSortedSummarizedExecutionResults => latestResultsSorted ?? Array.Empty<SummarizedExecutionResult>();

    public IWorkerReceiver GlobalBroadcaster { get; internal set; } = default!;

    public void StartWorkerFlow(string workloadName, int createWorkloadCount, int executeCount)
    {
        lock (ConnectionLock)
        {
            if (connections.Count == 0) return; // can not start.

            CurrentExecutionId = ExecutionId.NewExecutionId();

            var sorted = connections.Select(x => new SummarizedExecutionResult(x.Key, createWorkloadCount))
                .OrderBy(x => x.WorkerId)
                .ToArray();

            var dict = new Dictionary<WorkerId, int>();
            for (int i = 0; i < sorted.Length; i++)
            {
                var item = sorted[i];
                dict.TryAdd(item.WorkerId, i);
            }
            latestResults = dict;
            latestResultsSorted = sorted;

            RunningState = new RunningState(this, executeCount, connections.Select(x => x.Key));
            // TODO: pass parameters
            GlobalBroadcaster.CreateWorkloadAndSetup(CurrentExecutionId.Value, createWorkloadCount, workloadName, Array.Empty<(string, string)>());
            StateChanged?.Invoke();
        }
    }

    public void AddConnection(WorkerId workerId)
    {
        lock (ConnectionLock)
        {
            connections.Add(workerId, null);
            CurrentConnectingCount++;
            StateChanged?.Invoke();
        }
    }

    public void RemoveConnection(WorkerId workerId)
    {
        lock (ConnectionLock)
        {
            if (connections.Remove(workerId))
            {
                CurrentConnectingCount--;
            }

            if (RunningState != null)
            {
                RunningState.RemoveConnection(workerId);
                if (latestResults!.TryGetValue(workerId, out var i))
                {
                    // disconnected before complete is failed.
                    latestResultsSorted![i].TrySetStatus(ExecutionStatus.Failed);
                }
            }

            StateChanged?.Invoke();
        }
    }

    public void AddMetadata(WorkerId workerId, WorkloadInfo[] workloads, Dictionary<string, string> metadata)
    {
        lock (ConnectionLock)
        {
            if (connections.ContainsKey(workerId))
            {
                connections[workerId] = metadata;
            }

            // use latest one.
            if (this.workloadInfos.Length != workloads.Length)
            {
                this.workloadInfos = workloads;
            }
            StateChanged?.Invoke();
        }
    }

    public IReadOnlyDictionary<string, string> GetMetadata(WorkerId workerId)
    {
        if (connections.TryGetValue(workerId, out var dict))
        {
            return dict ?? EmptyMetadata;
        }
        return EmptyMetadata;
    }

    public void ReportExecuteResult(WorkerId workerId, ExecuteResult result)
    {
        lock (ConnectionLock)
        {
            if (latestResults != null)
            {
                if (latestResults.TryGetValue(workerId, out var i))
                {
                    latestResultsSorted![i].Add(result);
                }
            }

            StateChanged?.Invoke();
        }
    }

    public void ExecuteComplete(WorkerId workerId)
    {
        lock (ConnectionLock)
        {
            if (latestResults != null)
            {
                if (latestResults!.TryGetValue(workerId, out var i))
                {
                    latestResultsSorted![i].TrySetStatus(ExecutionStatus.Succeed);
                }
            }

            StateChanged?.Invoke();
        }
    }

    public void WorkflowCompleted()
    {
        lock (ConnectionLock)
        {
            // TODO: store log to inmemory history.

            RunningState = null; // complete.
            CurrentExecutionId = null;
            StateChanged?.Invoke();
        }
    }
}