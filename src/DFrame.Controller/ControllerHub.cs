using MagicOnion.Server.Hubs;

namespace DFrame.Controller;


// to Worker / to Controller
// CreateWorkloadAndSetup -> CreateWorkloadCompleteAsync
// Execute -> ExecuteCompleteAsync
// Teardown -> TeardownCompleteAsync

public sealed class ControllerHub : StreamingHubBase<IControllerHub, IWorkerReceiver>, IControllerHub
{
    readonly WorkerConnectionGroupContext workerConnectionContext;
    Guid workerId;

    public ControllerHub(WorkerConnectionGroupContext connectionContext)
    {
        this.workerConnectionContext = connectionContext;
    }

    protected override ValueTask OnConnecting()
    {
        workerId = Guid.Parse(Context.CallContext.RequestHeaders.GetValue("worker-id"));

        lock (workerConnectionContext.ConnectionLock)
        {
            var group = Group.AddAsync("global-masterhub-group").GetAwaiter().GetResult(); // always sync.
            var broadcaster = group.CreateBroadcaster<IWorkerReceiver>();
            workerConnectionContext.GlobalBroadcaster = broadcaster; // using new one:)
            workerConnectionContext.AddConnection(workerId);
        }

        return default;
    }

    protected override ValueTask OnDisconnected()
    {
        workerConnectionContext.RemoveConnection(workerId);
        return default;
    }

    public Task CreateWorkloadCompleteAsync(Guid executionId)
    {
        lock (workerConnectionContext.ConnectionLock)
        {
            var group = Group.AddAsync("running-group-" + executionId.ToString()).GetAwaiter().GetResult();
            workerConnectionContext.RunningState!.Broadcaster = group.CreateBroadcaster<IWorkerReceiver>();
            workerConnectionContext.RunningState.CreateWorkloadAndSetupComplete(workerId);
        }
        return Task.CompletedTask;
    }

    public Task ReportProgressAsync(ExecuteResult result)
    {
        // TODO: throw new NotImplementedException();
        return Task.CompletedTask;
    }

    public Task ExecuteCompleteAsync(ExecuteResult[] result)
    {
        // TODO:remove execute result?
        workerConnectionContext.AddExecuteResult(result);
        //workerConnectionContext.OnExecute.Done(workerId);

        workerConnectionContext.RunningState!.ExecuteComplete(workerId);
        return Task.CompletedTask;
    }

    public Task TeardownCompleteAsync()
    {
        workerConnectionContext.RunningState!.TeardownComplete(workerId);
        return Task.CompletedTask;
    }
}

public class RunningState
{
    readonly WorkerConnectionGroupContext context;
    readonly int executeCount;
    readonly HashSet<Guid> runningConnections;

    // State
    HashSet<Guid>? createWorkloadAndSetupCompletes;
    HashSet<Guid>? executeCompletes;
    HashSet<Guid>? teardownCompletes;

    public IWorkerReceiver Broadcaster { get; set; } = default!;

    public RunningState(WorkerConnectionGroupContext context, int executeCount, HashSet<Guid> connections)
    {
        this.executeCount = executeCount;
        this.runningConnections = connections.ToHashSet(); // create copy
        this.createWorkloadAndSetupCompletes = new HashSet<Guid>();
        this.context = context;
    }

    public void RemoveConnection(Guid guid)
    {
        lock (context.ConnectionLock)
        {
            runningConnections.Remove(guid);
            if (createWorkloadAndSetupCompletes != null)
            {
                createWorkloadAndSetupCompletes.Remove(guid);
                SignalState();
                return;
            }
            if (executeCompletes != null)
            {
                executeCompletes.Remove(guid);
                SignalState();
                return;
            }
            if (teardownCompletes != null)
            {
                teardownCompletes.Remove(guid);
                SignalState();
                return;
            }
        }
    }

    public void CreateWorkloadAndSetupComplete(Guid guid)
    {
        lock (context.ConnectionLock)
        {
            if (createWorkloadAndSetupCompletes == null) throw new InvalidOperationException("Invalid state.");
            createWorkloadAndSetupCompletes.Add(guid);
            SignalState();
        }
    }

    public void ExecuteComplete(Guid guid)
    {
        lock (context.ConnectionLock)
        {
            if (executeCompletes == null) throw new InvalidOperationException("Invalid state.");
            executeCompletes.Add(guid);
            SignalState();
        }
    }

    public void TeardownComplete(Guid guid)
    {
        lock (context.ConnectionLock)
        {
            if (teardownCompletes == null) throw new InvalidOperationException("Invalid state.");
            teardownCompletes.Add(guid);
            SignalState();
        }
    }

    void SignalState()
    {
        if (createWorkloadAndSetupCompletes != null && createWorkloadAndSetupCompletes.Count == runningConnections.Count)
        {
            createWorkloadAndSetupCompletes = null;
            executeCompletes = new HashSet<Guid>(); // setup next state.
            Broadcaster.Execute(executeCount);
            return;
        }
        if (executeCompletes != null && executeCompletes.Count == runningConnections.Count)
        {
            executeCompletes = null;
            teardownCompletes = new HashSet<Guid>(); // setup next state.
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

public class WorkerConnectionGroupContext
{
    public readonly object ConnectionLock = new object();
    readonly HashSet<Guid> connections = new HashSet<Guid>();

    public int CurrentConnectingCount { get; set; }
    public event Action<int>? OnConnectingCountChanged;
    public bool IsRunning => RunningState != null;
    public RunningState? RunningState { get; set; }
    public event Action<bool>? RunningStateChanged = null;

    // TODO:what's this???
    Guid? executionId = null;
    List<ExecuteResult> executeResult = default!;
    public IReadOnlyList<ExecuteResult> ExecuteResult => executeResult;

    public IWorkerReceiver GlobalBroadcaster { get; internal set; } = default!;


    public WorkerConnectionGroupContext()
    {
        this.connections = new HashSet<Guid>();
        this.executeResult = new List<ExecuteResult>();
    }

    public Guid[] StartWorkerFlow(string workloadName, int createWorkloadCount, int executeCount)
    {
        lock (ConnectionLock)
        {
            if (connections.Count == 0) return Array.Empty<Guid>(); // can not start.
            
            executionId = Guid.NewGuid();
            RunningState = new RunningState(this, executeCount, connections);
            GlobalBroadcaster.CreateWorkloadAndSetup(executionId.Value, createWorkloadCount, workloadName);
            return connections.ToArray(); // TODO:should return workerId!
        }
    }

    public void AddExecuteResult(ExecuteResult[] results)
    {
        lock (executeResult)
        {
            executeResult.AddRange(results);
        }
    }

    public void AddConnection(Guid guid)
    {
        lock (connections)
        {
            connections.Add(guid);
            CurrentConnectingCount++;
            OnConnectingCountChanged?.Invoke(CurrentConnectingCount);
        }
    }

    public void RemoveConnection(Guid guid)
    {
        lock (connections)
        {
            if (connections.Remove(guid))
            {
                CurrentConnectingCount--;
            }

            if (RunningState != null)
            {
                RunningState.RemoveConnection(guid);
            }

            OnConnectingCountChanged?.Invoke(CurrentConnectingCount);
        }
    }

    public void WorkflowCompleted()
    {
        RunningState = null; // complete.
        RunningStateChanged?.Invoke(false);
    }
}

internal class ConnectionDisconnectedException : Exception
{
    public Guid WorkerId { get; }

    public ConnectionDisconnectedException(Guid workerId)
    {
        WorkerId = workerId;
    }
}