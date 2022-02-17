using MessagePipe;
using Microsoft.AspNetCore.Components;

namespace DFrame.Pages;

public partial class Drawer : IDisposable
{
    [Inject] IScopedSubscriber<DrawerRequest> subscriber { get; set; } = default!;
    IDisposable? subscription;

    bool isShow;
    (string, string)[]? parameters;
    string? errorMessage;

    protected override void OnInitialized()
    {
        subscription = subscriber.Subscribe(async x =>
        {
            isShow = x.IsShow;
            parameters = x.Parameters;
            errorMessage = x.ErrorMessage;

            await InvokeAsync(StateHasChanged);
        });
    }

    public void Dispose()
    {
        subscription?.Dispose();
    }
}

public record DrawerRequest(bool IsShow, (string, string)[]? Parameters, string? ErrorMessage);