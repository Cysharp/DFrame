namespace DFrame.Controller;

// Note: this class is too complex, need refactoring.

// Singleton Global State.
public class DFrameControllerExecutionEngine : INotifyStateChanged
{
    static readonly IReadOnlyDictionary<string, string> EmptyMetadata = new Dictionary<string, string>();

    // Notify.
    public event Action? StateChanged;

    // TODO: lock to private.
    internal readonly object EngineSync = new object();

    readonly ILoggerFactory loggerFactory;

    readonly Dictionary<WorkerId, Dictionary<string, string>?> connections = new();
    WorkloadInfo[] workloadInfos = Array.Empty<WorkloadInfo>();

    public WorkloadInfo[] WorkloadInfos => workloadInfos;

    public int CurrentConnectingCount { get; private set; }
    public bool IsRunning => RunningState != null;


    WorkersRunningStateMachine? RunningState;

    public ExecutionId? CurrentExecutionId { get; private set; }

    Dictionary<WorkerId, int>? latestResults; // index of latestResultsSorted
    SummarizedExecutionResult[]? latestResultsSorted; // for performance reason, store stored array.

    public SummarizedExecutionResult[] LatestSortedSummarizedExecutionResults => latestResultsSorted ?? Array.Empty<SummarizedExecutionResult>();

    public IWorkerReceiver GlobalBroadcaster { get; internal set; } = default!;

    public DFrameControllerExecutionEngine(ILoggerFactory loggerFactory)
    {
        this.loggerFactory = loggerFactory;
    }

    public void StartWorkerFlow(string workloadName, int createWorkloadCount, int executeCount)
    {
        lock (EngineSync)
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

            RunningState = new WorkersRunningStateMachine(executeCount, connections.Select(x => x.Key), loggerFactory);
            // TODO: pass parameters
            GlobalBroadcaster.CreateWorkloadAndSetup(CurrentExecutionId.Value, createWorkloadCount, workloadName, Array.Empty<(string, string)>());
            StateChanged?.Invoke();
        }
    }

    public void AddConnection(WorkerId workerId)
    {
        lock (EngineSync)
        {
            connections.Add(workerId, null);
            CurrentConnectingCount++;
            StateChanged?.Invoke();
        }
    }

    public void RemoveConnection(WorkerId workerId)
    {
        lock (EngineSync)
        {
            if (connections.Remove(workerId))
            {
                CurrentConnectingCount--;
            }

            if (latestResults!.TryGetValue(workerId, out var i))
            {
                // disconnected before complete is failed.
                latestResultsSorted![i].TrySetStatus(ExecutionStatus.Failed);
            }

            if (RunningState != null)
            {
                if (RunningState.RemoveConnection(workerId))
                {
                    WorkflowCompleted();
                    return;
                }
            }

            StateChanged?.Invoke();
        }
    }

    public void AddMetadata(WorkerId workerId, WorkloadInfo[] workloads, Dictionary<string, string> metadata)
    {
        lock (EngineSync)
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
        lock (EngineSync)
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


    public void CreateWorkloadAndSetupComplete(WorkerId workerId, IWorkerReceiver broadcaster)
    {
        lock (EngineSync)
        {
            if (RunningState?.CreateWorkloadAndSetupComplete(workerId, broadcaster) ?? true)
            {
                WorkflowCompleted();
            }
        }
    }

    public void TeardownComplete(WorkerId workerId)
    {
        lock (EngineSync)
        {
            if (RunningState?.TeardownComplete(workerId) ?? true)
            {
                WorkflowCompleted();
            }
        }
    }

    public void ExecuteComplete(WorkerId workerId)
    {
        lock (EngineSync)
        {
            if (latestResults != null)
            {
                if (latestResults!.TryGetValue(workerId, out var i))
                {
                    latestResultsSorted![i].TrySetStatus(ExecutionStatus.Succeed);
                }
            }

            if (RunningState?.ExecuteComplete(workerId) ?? true)
            {
                WorkflowCompleted();
                return;
            }

            StateChanged?.Invoke();
        }
    }

    public void WorkflowCompleted()
    {
        lock (EngineSync)
        {
            // TODO: store log to inmemory history.

            RunningState = null; // complete.
            CurrentExecutionId = null;
            StateChanged?.Invoke();
        }
    }
}