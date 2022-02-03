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
        ConnectionGroupContext.StateChanged += ConnectionGroupContext_StateChanged;
        ConnectionGroupContext.OnExecuteProgress += ConnectionGroupContext_OnExecuteProgress;
    }

    public void Dispose()
    {
        ConnectionGroupContext.StateChanged -= ConnectionGroupContext_StateChanged;
        ConnectionGroupContext.OnExecuteProgress -= ConnectionGroupContext_OnExecuteProgress;
    }

    async void ConnectionGroupContext_StateChanged()
    {
        await InvokeAsync(StateHasChanged);
    }

    void ConnectionGroupContext_OnExecuteProgress(ExecuteResult obj)
    {
        // TODO:store logs?
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

    // TODO: rename?
    // concurrency(workload-per-worker)
    // totalrequestcount
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