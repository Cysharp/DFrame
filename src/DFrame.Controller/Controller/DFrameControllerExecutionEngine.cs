using MagicOnion.Server.Hubs;

namespace DFrame.Controller;

// Singleton Global State.
public class DFrameControllerExecutionEngine : INotifyStateChanged
{
    static readonly IReadOnlyDictionary<string, string> EmptyMetadata = new Dictionary<string, string>();

    // Notify.
    public event Action? StateChanged;

    readonly object EngineSync = new object();
    readonly ILoggerFactory loggerFactory;
    readonly IExecutionResultHistoryProvider historyProvider;

    // Global states
    readonly Dictionary<WorkerId, (Guid ConnectionId, Dictionary<string, string>? metaData)> connections = new();
    int connectionsCount; // cache field of connections.Count
    WorkloadInfo[] workloadInfos = Array.Empty<WorkloadInfo>();
    IGroup? globalGroup;

    // when running, keep this state
    WorkersRunningStateMachine? RunningState;

    // expose state for view
    public WorkloadInfo[] WorkloadInfos => workloadInfos;
    public int CurrentConnectingCount => connectionsCount;
    public bool IsRunning => RunningState != null;

    // storing latest inmemory.
    public ExecutionSummary? LatestExecutionSummary { get; private set; } = default;
    public SummarizedExecutionResult[] LatestSortedSummarizedExecutionResults { get; private set; } = Array.Empty<SummarizedExecutionResult>();

    public DFrameControllerExecutionEngine(ILoggerFactory loggerFactory, IExecutionResultHistoryProvider historyProvider)
    {
        this.loggerFactory = loggerFactory;
        this.historyProvider = historyProvider;
    }

    public void StartWorkerFlow(string workloadName, int concurrency, int totalRequestCount, int workerLimit, (string name, string value)[] parameters)
    {
        lock (EngineSync)
        {
            if (connections.Count == 0) return; // can not start.
            if (workerLimit <= 0) return;
            if (concurrency <= 0) return;

            if (globalGroup == null) throw new InvalidOperationException("GlobalGroup does not exists.");

            var workerCount = workerLimit;
            if (connections.Count < workerLimit)
            {
                workerCount = connections.Count;
            }

            // If totalRequestCount is lower than workers, concurrency(workload-count), reduce worker at first and after reduce concurrency.

            var createWorkloadCount = concurrency;
            int executeCountPerWorker = totalRequestCount / workerCount / concurrency;
            if (executeCountPerWorker == 0) executeCountPerWorker = 1;
            var executeCountPerWorkload = executeCountPerWorker / createWorkloadCount;
            if (executeCountPerWorkload == 0) executeCountPerWorkload = 1;

            var rest = totalRequestCount % (executeCountPerWorkload * createWorkloadCount * workerCount);

            var connetionIds = new Guid[workerCount];
            var sorted = connections
                .OrderBy(x => x.Key)
                .Take(workerCount)
                .Select((x, i) =>
                {
                    // evil side-effect
                    connetionIds[i] = x.Value.ConnectionId;
                    return new SummarizedExecutionResult(x.Key, createWorkloadCount)
                    {
                        executeCountPerWorkload = Enumerable.Repeat(executeCountPerWorkload, createWorkloadCount).ToArray()
                    };
                })
                .ToArray();

            var workloadIndex = 0;
            while (rest != 0)
            {
                foreach (var item in sorted)
                {
                    item.executeCountPerWorkload[workloadIndex] += 1;
                    rest--;
                    if (rest == 0) break;
                }
                workloadIndex++;
            }

            var executionId = ExecutionId.NewExecutionId();

            var summary = new ExecutionSummary
            {
                Workload = workloadName,
                ExecutionId = executionId,
                WorkerCount = workerCount,
                Concurrency = concurrency,
                WorkloadCount = workerCount * concurrency,
                TotalRequest = totalRequestCount,
                Parameters = parameters,
                StartTime = DateTime.UtcNow
            };

            RunningState = new WorkersRunningStateMachine(summary, sorted, loggerFactory);
            LatestExecutionSummary = summary;
            LatestSortedSummarizedExecutionResults = sorted;

            IWorkerReceiver broadcaster;
            if (connections.Count == workerCount)
            {
                broadcaster = globalGroup.CreateBroadcaster<IWorkerReceiver>();
            }
            else
            {
                broadcaster = globalGroup.CreateBroadcasterTo<IWorkerReceiver>(connetionIds);
            }

            broadcaster.CreateWorkloadAndSetup(executionId, createWorkloadCount, workloadName, parameters);
            StateChanged?.Invoke();
        }
    }

    public void AddConnection(WorkerId workerId, Guid connectionId, IGroup globalGroup)
    {
        lock (EngineSync)
        {
            this.globalGroup = globalGroup;
            connections.Add(workerId, (connectionId, null));
            connectionsCount++;
            StateChanged?.Invoke();
        }
    }

    public void RemoveConnection(WorkerId workerId)
    {
        lock (EngineSync)
        {
            if (connections.Remove(workerId))
            {
                connectionsCount--;
            }

            if (RunningState != null)
            {
                if (RunningState.RemoveConnection(workerId))
                {
                    WorkflowCompleted();
                    return;
                }

                if (connectionsCount == 0)
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
            RunningState?.ReportExecuteResult(workerId, result);

            // TODO:result has error message, write error to log.
            StateChanged?.Invoke();
        }
    }

    public void CreateWorkloadAndSetupComplete(WorkerId workerId, IWorkerReceiver broadcaster, IWorkerReceiver broadcastToSelf)
    {
        lock (EngineSync)
        {
            if (RunningState?.CreateWorkloadAndSetupComplete(workerId, broadcaster, broadcastToSelf) ?? true)
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
            var summary = LatestExecutionSummary;
            if (summary != null)
            {
                summary.RunningTime = LatestSortedSummarizedExecutionResults.Max(x => x.RunningTime);
                summary.SucceedSum = LatestSortedSummarizedExecutionResults.Sum(x => x.SucceedCount);
                summary.ErrorSum = LatestSortedSummarizedExecutionResults.Sum(x => x.ErrorCount);
                summary.RpsSum = LatestSortedSummarizedExecutionResults.Sum(x => x.Rps);

                historyProvider.AddNewResult(summary!, LatestSortedSummarizedExecutionResults);
            }
            RunningState = null; // complete.
            StateChanged?.Invoke();
        }
    }

    public void Cancel()
    {
        lock (EngineSync)
        {
            if (RunningState != null)
            {
                RunningState.CancelAll();
            }
        }
    }
}