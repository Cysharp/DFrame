using DFrame.Controller;
using MessagePipe;
using Microsoft.AspNetCore.Components;
using ObservableCollections;

namespace DFrame.Pages.Components;

public partial class Drawer : IDisposable
{
    [Inject] IScopedSubscriber<DrawerRequest> subscriber { get; set; } = default!;
    IDisposable? subscription;

    string? title;
    bool isShow;
    IReadOnlyList<KeyValuePair<string, string?>>? parameters;
    string? errorMessage;
    ISynchronizedView<string, string>? logView;
    Dictionary<WorkloadId, Dictionary<string, string>?>? results;

    protected override void OnInitialized()
    {
        subscription = subscriber.Subscribe(async x =>
        {
            title = x.Title;
            isShow = x.IsShow;
            parameters = x.Parameters;
            errorMessage = x.ErrorMessage;
            logView = x.LogView;
            results = x.Results;

            await InvokeAsync(StateHasChanged);
        });
    }

    void Close()
    {
        isShow = false;
    }

    public void Dispose()
    {
        subscription?.Dispose();
    }
}

public record DrawerRequest(
    string? Title,
    bool IsShow,
    IReadOnlyList<KeyValuePair<string, string?>>? Parameters,
    string? ErrorMessage, ISynchronizedView<string, string>? LogView,
    Dictionary<WorkloadId, Dictionary<string, string>?>? Results
);