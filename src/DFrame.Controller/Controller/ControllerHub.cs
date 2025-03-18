using Cysharp.Runtime.Multicast;
using MagicOnion.Server.Hubs;

namespace DFrame.Controller;

// to Worker / to Controller
// CreateWorkloadAndSetup -> CreateWorkloadCompleteAsync
// Execute -> ExecuteCompleteAsync
// Teardown -> TeardownCompleteAsync

public sealed class ControllerHub : StreamingHubBase<IControllerHub, IWorkerReceiver>, IControllerHub
{
    readonly DFrameControllerExecutionEngine engine;
    WorkerId workerId;
    IGroup<IWorkerReceiver>? executingGroup;

    public ControllerHub(DFrameControllerExecutionEngine engine, IMulticastGroupProvider groupProvider)
    {
        this.engine = engine;
    }

    protected override ValueTask OnConnecting()
    {
        // Connect process uses ConnectAsync
        return default;
    }

    protected override ValueTask OnDisconnected()
    {
        engine.RemoveConnection(workerId);
        return default;
    }

    public Task ConnectAsync(WorkloadInfo[] workloads, Dictionary<string, string> metadata)
    {
        workerId = WorkerId.Parse(Context.CallContext.RequestHeaders.GetValue("worker-id"));

        var workerInfo = new WorkerInfo(workerId, this.ConnectionId, DateTime.UtcNow, metadata);

        var group = Group.AddAsync("global-masterhub-group").GetAwaiter().GetResult(); // always sync.
        engine.AddConnection(workerInfo, workloads, group); // using new one:)

        return Task.CompletedTask;
    }

    public Task CreateWorkloadCompleteAsync(ExecutionId executionId)
    {
        executingGroup = Group.AddAsync("running-group-" + executionId.ToString()).GetAwaiter().GetResult();

        engine.CreateWorkloadAndSetupComplete(workerId, executingGroup.All, Client);

        return Task.CompletedTask;
    }

    public Task ReportProgressAsync(ExecuteResult result)
    {
        engine.ReportExecuteResult(workerId, result);
        return Task.CompletedTask;
    }

    public Task ReportProgressBatchedAsync(BatchedExecuteResult result)
    {
        engine.ReportExecuteResult(workerId, result);
        return Task.CompletedTask;
    }

    public Task ExecuteCompleteAsync(Dictionary<WorkloadId, Dictionary<string, string>?> results)
    {
        engine.ExecuteComplete(workerId, results);
        return Task.CompletedTask;
    }

    public Task TeardownCompleteAsync()
    {
        executingGroup?.RemoveAsync(Context);
        executingGroup = null;
        engine.TeardownComplete(workerId);
        return Task.CompletedTask;
    }
}