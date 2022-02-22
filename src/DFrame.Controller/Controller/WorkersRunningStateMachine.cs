namespace DFrame.Controller;

public class WorkersRunningStateMachine
{
    readonly HashSet<WorkerId> runningConnections;
    readonly ILogger<WorkersRunningStateMachine> logger;
    readonly ExecutionSummary executionSummary;
    readonly Dictionary<WorkerId, int> resultsIndex;
    readonly SummarizedExecutionResult[] resultsSorted; // for performance reason, store sorted array.

    // State
    HashSet<WorkerId>? createWorkloadAndSetupCompletes;
    HashSet<WorkerId>? executeCompletes;
    HashSet<WorkerId>? teardownCompletes;

    IWorkerReceiver? broadcaster;
    DateTime? executeBegin;

    public WorkersRunningStateMachine(ExecutionSummary summary, SummarizedExecutionResult[] sortedResultStore, ILoggerFactory loggerFactory)
    {
        this.executionSummary = summary;
        this.runningConnections = sortedResultStore.Select(x => x.WorkerId).ToHashSet(); // create copy
        this.createWorkloadAndSetupCompletes = new HashSet<WorkerId>();
        this.logger = loggerFactory.CreateLogger<WorkersRunningStateMachine>();

        // create result lookup
        var dict = new Dictionary<WorkerId, int>(sortedResultStore.Length);
        for (int i = 0; i < sortedResultStore.Length; i++)
        {
            var item = sortedResultStore[i];
            dict.TryAdd(item.WorkerId, i);
        }
        this.resultsIndex = dict;
        this.resultsSorted = sortedResultStore;
    }

    // return bool == true, state is completed.

    public bool RemoveConnection(WorkerId workerId)
    {
        logger.LogInformation($"Connection removing: {workerId}");

        runningConnections.Remove(workerId);

        // set status to fail.
        if (resultsIndex.TryGetValue(workerId, out var i))
        {
            resultsSorted[i].TrySetStatus(ExecutionStatus.Failed, null);
        }

        if (runningConnections.Count == 0)
        {
            return true;
        }

        if (createWorkloadAndSetupCompletes != null)
        {
            createWorkloadAndSetupCompletes.Remove(workerId);
            return SignalState();
        }
        if (executeCompletes != null)
        {
            executeCompletes.Remove(workerId);
            return SignalState();
        }
        if (teardownCompletes != null)
        {
            teardownCompletes.Remove(workerId);
            return SignalState();
        }

        // maybe invalid state.
        return true;
    }

    public bool CreateWorkloadAndSetupComplete(WorkerId workerId, IWorkerReceiver broadcaster, IWorkerReceiver broadcasterToSelf)
    {
        if (createWorkloadAndSetupCompletes == null) throw new InvalidOperationException("Invalid state.");
        this.broadcaster = broadcaster; // override latest(same)
        createWorkloadAndSetupCompletes.Add(workerId);

        if (resultsIndex.TryGetValue(workerId, out var i))
        {
            resultsSorted[i].executeBroadcasterToSelf = broadcasterToSelf;
        }

        return SignalState();
    }

    public void ReportExecuteResult(WorkerId workerId, ExecuteResult result)
    {
        if (resultsIndex.TryGetValue(workerId, out var i))
        {
            resultsSorted[i].Add(result);

            if (result.HasError)
            {
                if (executionSummary.ErrorSum == null)
                {
                    executionSummary.ErrorSum = 1;
                }
                else
                {
                    executionSummary.ErrorSum++;
                }
            }
            else
            {
                if (executionSummary.SucceedSum == null)
                {
                    executionSummary.SucceedSum = 1;
                }
                else
                {
                    executionSummary.SucceedSum++;
                }
            }

            if (executeBegin != null)
            {
                executionSummary.RunningTime = DateTime.UtcNow - executeBegin.Value;
            }
        }
    }

    public void ReportExecuteResult(WorkerId workerId, BatchedExecuteResult result)
    {
        if (resultsIndex.TryGetValue(workerId, out var i))
        {
            resultsSorted[i].Add(result);

            if (executionSummary.SucceedSum == null)
            {
                executionSummary.SucceedSum = 1;
            }
            else
            {
                executionSummary.SucceedSum += result.BatchedElapsed.Count;
            }

            if (executeBegin != null)
            {
                executionSummary.RunningTime = DateTime.UtcNow - executeBegin.Value;
            }
        }
    }

    public bool ExecuteComplete(WorkerId workerId, Dictionary<WorkloadId, Dictionary<string, string>?> results)
    {
        if (executeCompletes == null) throw new InvalidOperationException("Invalid state.");

        if (resultsIndex.TryGetValue(workerId, out var i))
        {
            resultsSorted[i].TrySetStatus(ExecutionStatus.Succeed, results);
        }

        executeCompletes.Add(workerId);
        return SignalState();
    }

    public bool TeardownComplete(WorkerId workerId)
    {
        if (teardownCompletes == null) throw new InvalidOperationException("Invalid state.");
        teardownCompletes.Add(workerId);
        return SignalState();
    }

    public void CancelAll()
    {
        logger.LogInformation($"Send cancel command to all workers.");
        broadcaster?.Stop();
    }

    bool SignalState()
    {
        if (broadcaster == null)
        {
            logger.LogInformation($"Detect invalid workflow, force complete.");
            return true;
        }

        if (createWorkloadAndSetupCompletes != null && createWorkloadAndSetupCompletes.Count == runningConnections.Count)
        {
            logger.LogInformation($"Workload {executionSummary.Workload} all {executionSummary.WorkerCount} workers {executionSummary.WorkloadCount} workload setup complete.");
            createWorkloadAndSetupCompletes = null;
            executeCompletes = new HashSet<WorkerId>(); // setup next state.
            executeBegin = DateTime.UtcNow;
            executionSummary.RunningTime = TimeSpan.Zero;
            executionSummary.SucceedSum = 0;
            executionSummary.ErrorSum = 0;
            executionSummary.RpsSum = 0;

            foreach (var item in resultsSorted)
            {
                item.InitExecuteBeginTime(executeBegin.Value);
                item.executeBroadcasterToSelf?.Execute(item.executeCountPerWorkload);
            }
            return false;
        }
        if (executeCompletes != null && executeCompletes.Count == runningConnections.Count)
        {
            logger.LogInformation($"Workload {executionSummary.Workload} all {executionSummary.WorkerCount} workers execute complete.");
            executeCompletes = null;
            teardownCompletes = new HashSet<WorkerId>(); // setup next state.
            broadcaster.Teardown();
            return false;
        }
        if (teardownCompletes != null && teardownCompletes.Count == runningConnections.Count)
        {
            logger.LogInformation($"Workload {executionSummary.Workload} all {executionSummary.WorkerCount} workers teardown complete.");
            teardownCompletes = null;
            return true;
        }

        return false; // keep running
    }
}