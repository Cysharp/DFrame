using MessagePipe;
using Microsoft.AspNetCore.Components;
using ObservableCollections;

namespace DFrame.Pages;

public partial class Drawer : IDisposable
{
    [Inject] IScopedSubscriber<DrawerRequest> subscriber { get; set; } = default!;
    IDisposable? subscription;

    bool isShow;
    (string, string)[]? parameters;
    string? errorMessage;
    ISynchronizedView<string, string>? logView;

    protected override void OnInitialized()
    {
        subscription = subscriber.Subscribe(async x =>
        {
            isShow = x.IsShow;
            parameters = x.Parameters;
            errorMessage = x.ErrorMessage;
            logView = x.LogView;

            await InvokeAsync(StateHasChanged);
        });
    }

    public void Dispose()
    {
        subscription?.Dispose();
    }
}

public record DrawerRequest(bool IsShow, (string, string)[]? Parameters, string? ErrorMessage, ISynchronizedView<string, string>? LogView);