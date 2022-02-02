using MessagePipe;
using Microsoft.AspNetCore.Components;

namespace DFrame.Controller.Pages;

public partial class Index : IDisposable
{
    [Inject]
    public WorkerConnectionGroupContext ConnectionGroupContext { get; set; } = default!;

    InputFormModel inputFormModel = new InputFormModel();

    int GetCurrentConnectingCount() => ConnectionGroupContext.CurrentConnectingCount;
    SummarizedExecutionResult[] GetRunnningResults() => ConnectionGroupContext.LatestSortedSummarizedExecutionResults;

    protected override void OnInitialized()
    {
        ConnectionGroupContext.OnConnectingCountChanged += ConnectionGroupContext_OnConnectingCountChanged;
        ConnectionGroupContext.OnExecuteProgress += ConnectionGroupContext_OnExecuteProgress;
        ConnectionGroupContext.OnWorkerExecuteCompleted += ConnectionGroupContext_OnWorkerExecuteCompleted;
        ConnectionGroupContext.RunningStateChanged += ConnectionGroupContext_RunningStateChanged;
    }

    public void Dispose()
    {
        ConnectionGroupContext.OnConnectingCountChanged -= ConnectionGroupContext_OnConnectingCountChanged;
        ConnectionGroupContext.OnExecuteProgress -= ConnectionGroupContext_OnExecuteProgress;
        ConnectionGroupContext.OnWorkerExecuteCompleted -= ConnectionGroupContext_OnWorkerExecuteCompleted;
        ConnectionGroupContext.RunningStateChanged -= ConnectionGroupContext_RunningStateChanged;
    }

    async void ConnectionGroupContext_RunningStateChanged(bool obj)
    {
        await InvokeAsync(StateHasChanged);
    }

    async void ConnectionGroupContext_OnWorkerExecuteCompleted()
    {
        await InvokeAsync(StateHasChanged);
    }

    async void ConnectionGroupContext_OnConnectingCountChanged(int count)
    {
        await InvokeAsync(StateHasChanged);
    }


    async void ConnectionGroupContext_OnExecuteProgress(ExecuteResult obj)
    {
        // store logs?
        await InvokeAsync(() =>
        {
            StateHasChanged();
        });
    }

    void HandleSubmit()
    {
        if (inputFormModel.WorkloadName == null)
        {
            // Invalid...
            return;
        }

        if (ConnectionGroupContext.IsRunning) // can not invoke
        {
            return;
        }

        ConnectionGroupContext.StartWorkerFlow(inputFormModel.WorkloadName, inputFormModel.WorkloadPerWorker, inputFormModel.ExecutePerWorkload);
    }

    public class InputFormModel
    {
        public string? WorkloadName { get; set; }
        public int WorkloadPerWorker { get; set; } = 1;
        public int ExecutePerWorkload { get; set; } = 1;
    }
}

public record RunnningStatus
{
    public WorkerId WorkerId { get; set; }
}