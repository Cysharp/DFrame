using MagicOnion.Server.Hubs;

namespace DFrame.Controller;

// Singleton Global State.
public class DFrameControllerExecutionEngine : INotifyStateChanged
{
    // Notify.
    public event Action? StateChanged;

    readonly object EngineSync = new object();
    readonly ILoggerFactory loggerFactory;
    readonly IExecutionResultHistoryProvider historyProvider;

    // Global states
    readonly Dictionary<WorkerId, WorkerInfo> connections = new();
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
            if (workerLimit == 0) return;

            if (globalGroup == null) throw new InvalidOperationException("GlobalGroup does not exists.");

            if (connections.Count < workerLimit)
            {
                workerLimit = connections.Count;
            }

            var createWorkloadCount = concurrency;
            int executeCountPerWorker = totalRequestCount / workerLimit / concurrency;

            var connetionIds = new Guid[workerLimit];
            var sorted = connections
                .Select((x, i) =>
                {
                    // evil side-effect
                    connetionIds[i] = x.Value.ConnectionId;
                    return new SummarizedExecutionResult(x.Key, createWorkloadCount);
                })
                .OrderBy(x => x.WorkerId)
                .Take(workerLimit)
                .ToArray();

            var executionId = ExecutionId.NewExecutionId();

            var summary = new ExecutionSummary
            {
                Workload = workloadName,
                ExecutionId = executionId,
                WorkerCount = workerLimit,
                Concurrency = concurrency,
                WorkloadCount = workerLimit * concurrency,
                TotalRequest = workerLimit * concurrency * executeCountPerWorker,
                Parameters = parameters,
                StartTime = DateTime.UtcNow
            };

            RunningState = new WorkersRunningStateMachine(summary, executeCountPerWorker, sorted, loggerFactory);
            LatestExecutionSummary = summary;
            LatestSortedSummarizedExecutionResults = sorted;

            IWorkerReceiver broadcaster;
            if (connections.Count == workerLimit)
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

    internal void AddConnection(WorkerInfo workerInfo, WorkloadInfo[] workloads, IGroup globalGroup)
    {
        lock (EngineSync)
        {
            this.globalGroup = globalGroup;

            connections.Add(workerInfo.WorkerId, workerInfo);
            connectionsCount++;

            // use latest one.
            if (this.workloadInfos.Length != workloads.Length)
            {
                this.workloadInfos = workloads;
            }

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

    internal WorkerInfo[] GetWorkerInfos()
    {
        lock (EngineSync)
        {
            return connections.Select(x => x.Value).ToArray();
        }
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