namespace DFrame.Controller;

public class WorkersRunningStateMachine
{
    readonly int executeCount;
    readonly HashSet<WorkerId> runningConnections;
    readonly ILogger<WorkersRunningStateMachine> logger;

    // State
    HashSet<WorkerId>? createWorkloadAndSetupCompletes;
    HashSet<WorkerId>? executeCompletes;
    HashSet<WorkerId>? teardownCompletes;

    IWorkerReceiver? broadcaster;

    public WorkersRunningStateMachine(int executeCount, IEnumerable<WorkerId> connections, ILoggerFactory loggerFactory)
    {
        this.executeCount = executeCount;
        this.runningConnections = connections.ToHashSet(); // create copy
        this.createWorkloadAndSetupCompletes = new HashSet<WorkerId>();
        this.logger = loggerFactory.CreateLogger<WorkersRunningStateMachine>();
    }

    // return bool == true, state is completed.

    public bool RemoveConnection(WorkerId workerId)
    {
        logger.LogInformation($"Connection removing: {workerId}");

        runningConnections.Remove(workerId);
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

    public bool CreateWorkloadAndSetupComplete(WorkerId workerId, IWorkerReceiver broadcaster)
    {
        if (createWorkloadAndSetupCompletes == null) throw new InvalidOperationException("Invalid state.");
        this.broadcaster = broadcaster; // override latest(same)
        createWorkloadAndSetupCompletes.Add(workerId);
        return SignalState();
    }

    public bool ExecuteComplete(WorkerId workerId)
    {
        if (executeCompletes == null) throw new InvalidOperationException("Invalid state.");
        executeCompletes.Add(workerId);
        return SignalState();
    }

    public bool TeardownComplete(WorkerId workerId)
    {
        if (teardownCompletes == null) throw new InvalidOperationException("Invalid state.");
        teardownCompletes.Add(workerId);
        return SignalState();
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
            logger.LogInformation($"All workers workload setup complete.");
            createWorkloadAndSetupCompletes = null;
            executeCompletes = new HashSet<WorkerId>(); // setup next state.
            broadcaster.Execute(executeCount);
            return false;
        }
        if (executeCompletes != null && executeCompletes.Count == runningConnections.Count)
        {
            logger.LogInformation($"All workers execute complete.");
            executeCompletes = null;
            teardownCompletes = new HashSet<WorkerId>(); // setup next state.
            broadcaster.Teardown();
            return false;
        }
        if (teardownCompletes != null && teardownCompletes.Count == runningConnections.Count)
        {
            logger.LogInformation($"All workers teardown complete.");
            teardownCompletes = null;
            return true;
        }

        return false; // keep running
    }
}