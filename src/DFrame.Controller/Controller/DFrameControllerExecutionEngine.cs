using MagicOnion.Server.Hubs;

namespace DFrame.Controller;

// Singleton Global State.
public class DFrameControllerExecutionEngine : INotifyStateChanged
{
    // Notify.
    public event Action? StateChanged;

    readonly object EngineSync = new object();
    readonly ILogger<DFrameControllerExecutionEngine> logger;
    readonly ILoggerFactory loggerFactory;
    readonly IExecutionResultHistoryProvider historyProvider;
    readonly DFrameControllerOptions options;

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

    public DFrameControllerExecutionEngine(ILoggerFactory loggerFactory, IExecutionResultHistoryProvider historyProvider, DFrameControllerOptions options)
    {
        this.loggerFactory = loggerFactory;
        this.logger = loggerFactory.CreateLogger<DFrameControllerExecutionEngine>();
        this.historyProvider = historyProvider;
        this.options = options;
    }

    public bool StartWorkerFlow(string workloadName, int concurrency, long totalRequestCount, int workerLimit, KeyValuePair<string, string?>[] parameters)
    {
        lock (EngineSync)
        {
            if (connections.Count == 0) return false; // can not start.
            if (workerLimit <= 0) return false;
            if (concurrency <= 0) return false;
            if (IsRunning) return false;

            if (!workloadInfos.Any(x => x.Name == workloadName)) throw new InvalidOperationException($"Workload is not found. Name:{workloadName}");

            if (globalGroup == null) throw new InvalidOperationException("GlobalGroup does not exists.");

            var workerCount = workerLimit;
            if (connections.Count < workerLimit)
            {
                workerCount = connections.Count;
            }

            // If totalRequestCount is lower than workers, concurrency(workload-count), reduce worker at first and after reduce concurrency.
            if (totalRequestCount < workerCount)
            {
                workerCount = (int)totalRequestCount;
            }

            var createWorkloadCount = concurrency;
            if (totalRequestCount < createWorkloadCount * workerCount)
            {
                createWorkloadCount = (int)totalRequestCount / workerCount; // concurrency * workerCount (+ rest) = totalRequestCount
            }

            var executeCountPerWorkload = totalRequestCount / (createWorkloadCount * workerCount);
            if (executeCountPerWorkload == 0) executeCountPerWorkload = 1;

            var rest = (totalRequestCount == long.MaxValue)
                ? 0
                : totalRequestCount - (executeCountPerWorkload * createWorkloadCount * workerCount);

            var connectionIds = new Guid[workerCount];
            var sorted = connections
                .OrderBy(x => x.Key)
                .Take(workerCount)
                .Select((x, i) =>
                {
                    // evil side-effect
                    connectionIds[i] = x.Value.ConnectionId;
                    return new SummarizedExecutionResult(x.Key, createWorkloadCount, x.Value.Metadata, options)
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
                Concurrency = createWorkloadCount,
                WorkloadCount = workerCount * createWorkloadCount,
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
                broadcaster = globalGroup.CreateBroadcasterTo<IWorkerReceiver>(connectionIds);
            }

            broadcaster.CreateWorkloadAndSetup(executionId, createWorkloadCount, workloadName, parameters!);
            StateChanged?.Invoke();
        }

        return true;
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

            if (result.HasError)
            {
                logger.LogError($"Received client {workerId} error.{Environment.NewLine}{result.ErrorMessage}");
            }

            StateChanged?.Invoke();
        }
    }

    public void ReportExecuteResult(WorkerId workerId, BatchedExecuteResult result)
    {
        lock (EngineSync)
        {
            RunningState?.ReportExecuteResult(workerId, result);
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

    public void ExecuteComplete(WorkerId workerId, Dictionary<WorkloadId, Dictionary<string, string>?> results)
    {
        lock (EngineSync)
        {
            if (RunningState?.ExecuteComplete(workerId, results) ?? true)
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
                summary.TotalRequest = LatestSortedSummarizedExecutionResults.Sum(x => x.CompleteCount);

                try
                {
                    historyProvider.AddNewResult(summary!, LatestSortedSummarizedExecutionResults);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Error on add history provider to log.");
                }
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