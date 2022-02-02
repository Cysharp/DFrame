namespace DFrame.Controller;

// Singleton Global State.
public class WorkerConnectionGroupContext
{
    public readonly object ConnectionLock = new object();
    readonly HashSet<WorkerId> connections = new HashSet<WorkerId>();

    public int CurrentConnectingCount { get; private set; }
    public bool IsRunning => RunningState != null;
    public RunningState? RunningState { get; private set; }
    public ExecutionId? CurrentExecutionId { get; private set; }

    Dictionary<WorkerId, int>? latestResults; // index of latestResultsSorted
    SummarizedExecutionResult[]? latestResultsSorted; // for performance reason, store stored array.

    public SummarizedExecutionResult[] LatestSortedSummarizedExecutionResults => latestResultsSorted ?? Array.Empty<SummarizedExecutionResult>();

    // Notify.
    public event Action<int>? OnConnectingCountChanged;
    public event Action<ExecuteResult>? OnExecuteProgress;
    public event Action? OnWorkerExecuteCompleted = null;
    public event Action<bool>? RunningStateChanged = null;

    public IWorkerReceiver GlobalBroadcaster { get; internal set; } = default!;

    public void StartWorkerFlow(string workloadName, int createWorkloadCount, int executeCount)
    {
        lock (ConnectionLock)
        {
            if (connections.Count == 0) return; // can not start.

            CurrentExecutionId = ExecutionId.NewExecutionId();

            var sorted = connections.Select(x => new SummarizedExecutionResult(x, createWorkloadCount))
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

            RunningState = new RunningState(this, executeCount, connections);
            GlobalBroadcaster.CreateWorkloadAndSetup(CurrentExecutionId.Value, createWorkloadCount, workloadName);
            RunningStateChanged?.Invoke(true);
        }
    }

    public void AddConnection(WorkerId workerId)
    {
        lock (connections)
        {
            connections.Add(workerId);
            CurrentConnectingCount++;
            OnConnectingCountChanged?.Invoke(CurrentConnectingCount);
        }
    }

    public void RemoveConnection(WorkerId workerId)
    {
        lock (connections)
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

            OnConnectingCountChanged?.Invoke(CurrentConnectingCount);
        }
    }

    public void ReportExecuteResult(WorkerId workerId, ExecuteResult result)
    {
        lock (connections)
        {
            if (latestResults != null)
            {
                if (latestResults.TryGetValue(workerId, out var i))
                {
                    latestResultsSorted![i].Add(result);
                }
            }

            OnExecuteProgress?.Invoke(result); // send latest info
        }
    }

    public void ExecuteComplete(WorkerId workerId)
    {
        lock (connections)
        {
            if (latestResults != null)
            {
                if (latestResults!.TryGetValue(workerId, out var i))
                {
                    latestResultsSorted![i].TrySetStatus(ExecutionStatus.Succeed);
                }
            }
            OnWorkerExecuteCompleted?.Invoke();
        }
    }

    public void WorkflowCompleted()
    {
        lock (connections)
        {
            // TODO: store log to inmemory history.

            RunningState = null; // complete.
            CurrentExecutionId = null;
            RunningStateChanged?.Invoke(false);
        }
    }
}