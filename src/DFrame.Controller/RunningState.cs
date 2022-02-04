namespace DFrame.Controller;

public class RunningState
{
    readonly WorkerConnectionGroupContext context;
    readonly int executeCount;
    readonly HashSet<WorkerId> runningConnections;

    // State
    HashSet<WorkerId>? createWorkloadAndSetupCompletes;
    HashSet<WorkerId>? executeCompletes;
    HashSet<WorkerId>? teardownCompletes;

    public IWorkerReceiver Broadcaster { get; set; } = default!;

    public RunningState(WorkerConnectionGroupContext context, int executeCount, IEnumerable<WorkerId> connections)
    {
        this.executeCount = executeCount;
        this.runningConnections = connections.ToHashSet(); // create copy
        this.createWorkloadAndSetupCompletes = new HashSet<WorkerId>();
        this.context = context;
    }

    public void RemoveConnection(WorkerId workerId)
    {
        lock (context.ConnectionLock)
        {
            runningConnections.Remove(workerId);
            if (createWorkloadAndSetupCompletes != null)
            {
                createWorkloadAndSetupCompletes.Remove(workerId);
                SignalState();
                return;
            }
            if (executeCompletes != null)
            {
                executeCompletes.Remove(workerId);
                SignalState();
                return;
            }
            if (teardownCompletes != null)
            {
                teardownCompletes.Remove(workerId);
                SignalState();
                return;
            }
        }
    }

    public void CreateWorkloadAndSetupComplete(WorkerId workerId)
    {
        lock (context.ConnectionLock)
        {
            if (createWorkloadAndSetupCompletes == null) throw new InvalidOperationException("Invalid state.");
            createWorkloadAndSetupCompletes.Add(workerId);
            SignalState();
        }
    }

    public void ExecuteComplete(WorkerId workerId)
    {
        lock (context.ConnectionLock)
        {
            if (executeCompletes == null) throw new InvalidOperationException("Invalid state.");
            executeCompletes.Add(workerId);
            SignalState();
        }
    }

    public void TeardownComplete(WorkerId workerId)
    {
        lock (context.ConnectionLock)
        {
            if (teardownCompletes == null) throw new InvalidOperationException("Invalid state.");
            teardownCompletes.Add(workerId);
            SignalState();
        }
    }

    void SignalState()
    {
        if (Broadcaster == null)
        {
            // force complete
            context.WorkflowCompleted();
            return;
        }
        if (createWorkloadAndSetupCompletes != null && createWorkloadAndSetupCompletes.Count == runningConnections.Count)
        {
            createWorkloadAndSetupCompletes = null;
            executeCompletes = new HashSet<WorkerId>(); // setup next state.
            Broadcaster.Execute(executeCount);
            return;
        }
        if (executeCompletes != null && executeCompletes.Count == runningConnections.Count)
        {
            executeCompletes = null;
            teardownCompletes = new HashSet<WorkerId>(); // setup next state.
            Broadcaster.Teardown();
            return;
        }
        if (teardownCompletes != null && teardownCompletes.Count == runningConnections.Count)
        {
            teardownCompletes = null;
            context.WorkflowCompleted();
            return;
        }
    }
}
