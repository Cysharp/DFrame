using System.Text.Json;
using Microsoft.JSInterop;

namespace DFrame.Utilities;

public class LocalStorageAccessor
{
    readonly IJSRuntime jsRuntime;

    public LocalStorageAccessor(IJSRuntime jsRuntime)
    {
        this.jsRuntime = jsRuntime;
    }

    public async ValueTask<(bool, T)> TryGetItemAsync<T>(string key, CancellationToken cancellationToken)
    {
        var v = await jsRuntime.InvokeAsync<string>("localStorage.getItem", cancellationToken, key);

        if (v != null)
        {
            return (true, JsonSerializer.Deserialize<T>(v)!);
        }
        else
        {
            return (false, default(T)!);
        }
    }

    public async ValueTask RemoveItemAsync(string key, CancellationToken cancellationToken)
    {
        await jsRuntime.InvokeVoidAsync("localStorage.removeItem", cancellationToken, key);
    }

    public async ValueTask SetItemAsync<T>(string key, T value, CancellationToken cancellationToken)
    {
        var json = JsonSerializer.Serialize(value);
        await jsRuntime.InvokeVoidAsync("localStorage.setItem", cancellationToken, new object[] { key, json });
    }
}