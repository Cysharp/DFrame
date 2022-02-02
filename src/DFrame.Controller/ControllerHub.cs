using MagicOnion.Server.Hubs;
using MessagePipe;

namespace DFrame.Controller;

// to Worker / to Controller
// CreateWorkloadAndSetup -> CreateWorkloadCompleteAsync
// Execute -> ExecuteCompleteAsync
// Teardown -> TeardownCompleteAsync

public sealed class ControllerHub : StreamingHubBase<IControllerHub, IWorkerReceiver>, IControllerHub
{
    readonly WorkerConnectionGroupContext workerConnectionContext;
    WorkerId workerId;

    public ControllerHub(WorkerConnectionGroupContext connectionContext)
    {
        this.workerConnectionContext = connectionContext;
    }

    protected override ValueTask OnConnecting()
    {
        workerId = WorkerId.Parse(Context.CallContext.RequestHeaders.GetValue("worker-id"));

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

    public Task CreateWorkloadCompleteAsync(ExecutionId executionId)
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
        workerConnectionContext.ReportExecuteResult(workerId, result);
        return Task.CompletedTask;
    }

    public Task ExecuteCompleteAsync()
    {
        workerConnectionContext.ExecuteComplete(workerId);
        workerConnectionContext.RunningState!.ExecuteComplete(workerId);
        return Task.CompletedTask;
    }

    public Task TeardownCompleteAsync()
    {
        workerConnectionContext.RunningState!.TeardownComplete(workerId);
        return Task.CompletedTask;
    }
}