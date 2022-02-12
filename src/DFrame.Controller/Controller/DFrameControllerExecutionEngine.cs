using MagicOnion.Server.Hubs;

namespace DFrame.Controller;

// Note: this class is too complex, need refactoring.

// Singleton Global State.
public class DFrameControllerExecutionEngine : INotifyStateChanged
{
    static readonly IReadOnlyDictionary<string, string> EmptyMetadata = new Dictionary<string, string>();

    // Notify.
    public event Action? StateChanged;
    readonly object EngineSync = new object();

    readonly ILoggerFactory loggerFactory;

    readonly Dictionary<WorkerId, (Guid ConnectionId, Dictionary<string, string>? metaData)> connections = new();
    WorkloadInfo[] workloadInfos = Array.Empty<WorkloadInfo>();

    public WorkloadInfo[] WorkloadInfos => workloadInfos;

    public int CurrentConnectingCount { get; private set; }
    public bool IsRunning => RunningState != null;

    IGroup? globalGroup;
    WorkersRunningStateMachine? RunningState;

    public ExecutionId? CurrentExecutionId { get; private set; }
    public int? CurrentExecutingWorkloadCount { get; set; }

    Dictionary<WorkerId, int>? latestResults; // index of latestResultsSorted
    SummarizedExecutionResult[]? latestResultsSorted; // for performance reason, store stored array.

    public SummarizedExecutionResult[] LatestSortedSummarizedExecutionResults => latestResultsSorted ?? Array.Empty<SummarizedExecutionResult>();

    public DFrameControllerExecutionEngine(ILoggerFactory loggerFactory)
    {
        this.loggerFactory = loggerFactory;
    }

    public void StartWorkerFlow(string workloadName, int concurrency, int totalRequestCount, int workerLimit, (string name, string value)[] parameters)
    {
        lock (EngineSync)
        {
            if (connections.Count == 0) return; // can not start.
            if (globalGroup == null) throw new InvalidOperationException("GlobalGroup does not exists.");

            if (connections.Count < workerLimit)
            {
                workerLimit = connections.Count;
            }

            var createWorkloadCount = concurrency;
            int executeCountPerWorker = totalRequestCount / workerLimit;

            CurrentExecutionId = ExecutionId.NewExecutionId();
            CurrentExecutingWorkloadCount = executeCountPerWorker * workerLimit * concurrency;

            var sorted = connections.Select(x => new SummarizedExecutionResult(x.Key, x.Value.ConnectionId, createWorkloadCount))
                .OrderBy(x => x.WorkerId)
                .Take(workerLimit)
                .ToArray();

            var dict = new Dictionary<WorkerId, int>();
            for (int i = 0; i < sorted.Length; i++)
            {
                var item = sorted[i];
                dict.TryAdd(item.WorkerId, i);
            }
            latestResults = dict;
            latestResultsSorted = sorted;

            RunningState = new WorkersRunningStateMachine(executeCountPerWorker, sorted.Select(x => x.WorkerId), loggerFactory);

            IWorkerReceiver broadcaster;
            if (connections.Count == workerLimit)
            {
                broadcaster = globalGroup.CreateBroadcaster<IWorkerReceiver>();
            }
            else
            {
                broadcaster = globalGroup.CreateBroadcasterTo<IWorkerReceiver>(sorted.Select(x => x.ConnectionId).ToArray());
            }

            broadcaster.CreateWorkloadAndSetup(CurrentExecutionId.Value, createWorkloadCount, workloadName, parameters);
            StateChanged?.Invoke();
        }
    }

    public void AddConnection(WorkerId workerId, Guid connectionId, IGroup globalGroup)
    {
        lock (EngineSync)
        {
            this.globalGroup = globalGroup;
            connections.Add(workerId, (connectionId, null));
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

            if (latestResults != null)
            {
                if (latestResults.TryGetValue(workerId, out var i))
                {
                    // disconnected before complete is failed.
                    latestResultsSorted![i].TrySetStatus(ExecutionStatus.Failed);
                }
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
                var (id, _) = connections[workerId];
                connections[workerId] = (id, metadata);
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
            return dict.metaData ?? EmptyMetadata;
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

            // TODO:result has error message, write error to log.

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
            CurrentExecutingWorkloadCount = null;
            StateChanged?.Invoke();
        }
    }
}