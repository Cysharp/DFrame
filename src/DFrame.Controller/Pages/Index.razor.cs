using MessagePipe;
using Microsoft.AspNetCore.Components;

namespace DFrame.Controller.Pages;

public partial class Index : IDisposable
{
    [Inject]
    public WorkerConnectionGroupContext ConnectionGroupContext { get; set; } = default!;

    InputFormModel inputFormModel = new InputFormModel();
    int currentConnectionCount;
    Guid[] runnningConnections = Array.Empty<Guid>(); // TODO:more info...!

    protected override void OnInitialized()
    {
        currentConnectionCount = ConnectionGroupContext.CurrentConnectingCount;
        ConnectionGroupContext.OnConnectingCountChanged += Context_OnConnectingCountChanged;
    }

    public void Dispose()
    {
        ConnectionGroupContext.OnConnectingCountChanged -= Context_OnConnectingCountChanged;
    }

    private async void Context_OnConnectingCountChanged(int count)
    {
        await InvokeAsync(() =>
        {
            currentConnectionCount = count;
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

        runnningConnections = ConnectionGroupContext.StartWorkerFlow(inputFormModel.WorkloadName, inputFormModel.WorkloadPerWorker, inputFormModel.ExecutePerWorkload);
    }

    public class InputFormModel
    {
        public string? WorkloadName { get; set; }
        public int WorkloadPerWorker { get; set; } = 1;
        public int ExecutePerWorkload { get; set; } = 1;
    }
}