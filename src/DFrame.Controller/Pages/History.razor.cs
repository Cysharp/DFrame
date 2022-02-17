using DFrame.Controller;
using Microsoft.AspNetCore.Components;

namespace DFrame.Pages;

public partial class History : IDisposable
{
    [Inject] IExecutionResultHistoryProvider historyProvider { get; set; } = default!;

    IReadOnlyList<ExecutionSummary> results = default!;

    protected override void OnInitialized()
    {
        historyProvider.NotifyCountChanged += HistoryProvider_NotifyCountChanged;
        results = historyProvider.GetList();
    }

    private async void HistoryProvider_NotifyCountChanged()
    {
        results = historyProvider.GetList();
        await InvokeAsync(StateHasChanged);
    }

    public void Dispose()
    {
        historyProvider.NotifyCountChanged -= HistoryProvider_NotifyCountChanged;
    }
}
