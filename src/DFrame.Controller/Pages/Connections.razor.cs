using DFrame.Controller;
using Microsoft.AspNetCore.Components;

namespace DFrame.Pages;

public partial class Connections : IDisposable
{
    [Inject] DFrameControllerExecutionEngine engine { get; set; } = default!;

    WorkerInfo[] workerInfos = default!;

    protected override void OnInitialized()
    {
        workerInfos = engine.GetWorkerInfos();
        engine.StateChanged += Engine_StateChanged;
    }

    private async void Engine_StateChanged()
    {
        await InvokeAsync(() =>
        {
            workerInfos = engine.GetWorkerInfos();
            StateHasChanged();
        });
    }

    public void Dispose()
    {
        engine.StateChanged -= Engine_StateChanged;
    }
}
