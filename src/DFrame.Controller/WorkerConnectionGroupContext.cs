namespace DFrame.Controller;

// Singleton Global State.
public class WorkerConnectionGroupContext
{
    public readonly object ConnectionLock = new object();
    readonly HashSet<WorkerId> connections = new HashSet<WorkerId>();

    public int CurrentConnectingCount { get; set; }
    public event Action<int>? OnConnectingCountChanged;
    public event Action<ExecuteResult>? OnExecuteProgress;

    public bool IsRunning => RunningState != null;
    public RunningState? RunningState { get; set; }
    public event Action<bool>? RunningStateChanged = null;

    ExecutionId? currentExecutionId = null;

    public IWorkerReceiver GlobalBroadcaster { get; internal set; } = default!;

    public WorkerId[] StartWorkerFlow(string workloadName, int createWorkloadCount, int executeCount)
    {
        lock (ConnectionLock)
        {
            if (connections.Count == 0) return Array.Empty<WorkerId>(); // can not start.

            currentExecutionId = ExecutionId.NewExecutionId();
            RunningState = new RunningState(this, executeCount, connections);
            GlobalBroadcaster.CreateWorkloadAndSetup(currentExecutionId.Value, createWorkloadCount, workloadName);
            return connections.ToArray();
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
            }

            OnConnectingCountChanged?.Invoke(CurrentConnectingCount);
        }
    }

    public void ReportExecuteResult(ExecuteResult result)
    {
        OnExecuteProgress?.Invoke(result);
    }

    public void WorkflowCompleted()
    {
        RunningState = null; // complete.
        RunningStateChanged?.Invoke(false);
    }
}