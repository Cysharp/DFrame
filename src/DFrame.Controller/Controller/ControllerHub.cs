﻿using MagicOnion.Server.Hubs;
using MessagePipe;

namespace DFrame.Controller;

// to Worker / to Controller
// CreateWorkloadAndSetup -> CreateWorkloadCompleteAsync
// Execute -> ExecuteCompleteAsync
// Teardown -> TeardownCompleteAsync

public sealed class ControllerHub : StreamingHubBase<IControllerHub, IWorkerReceiver>, IControllerHub
{
    readonly DFrameControllerExecutionEngine engine;
    WorkerId workerId;

    public ControllerHub(DFrameControllerExecutionEngine engine)
    {
        this.engine = engine;
    }

    protected override ValueTask OnConnecting()
    {
        workerId = WorkerId.Parse(Context.CallContext.RequestHeaders.GetValue("worker-id"));

        lock (engine.EngineSync)
        {
            var group = Group.AddAsync("global-masterhub-group").GetAwaiter().GetResult(); // always sync.
            var broadcaster = group.CreateBroadcaster<IWorkerReceiver>();
            engine.GlobalBroadcaster = broadcaster; // using new one:)
            engine.AddConnection(workerId);
        }

        return default;
    }

    protected override ValueTask OnDisconnected()
    {
        engine.RemoveConnection(workerId);
        return default;
    }

    public Task InitializeMetadataAsync(WorkloadInfo[] workloads, Dictionary<string, string> metadata)
    {
        engine.AddMetadata(workerId, workloads, metadata);
        return Task.CompletedTask;
    }

    public Task CreateWorkloadCompleteAsync(ExecutionId executionId)
    {
        lock (engine.EngineSync)
        {
            var group = Group.AddAsync("running-group-" + executionId.ToString()).GetAwaiter().GetResult();
            var broadcaster = group.CreateBroadcaster<IWorkerReceiver>();
            engine.CreateWorkloadAndSetupComplete(workerId, broadcaster);
        }
        return Task.CompletedTask;
    }

    public Task ReportProgressAsync(ExecuteResult result)
    {
        engine.ReportExecuteResult(workerId, result);
        return Task.CompletedTask;
    }

    public Task ExecuteCompleteAsync()
    {
        engine.ExecuteComplete(workerId);
        return Task.CompletedTask;
    }

    public Task TeardownCompleteAsync()
    {
        engine.TeardownComplete(workerId);
        return Task.CompletedTask;
    }
}