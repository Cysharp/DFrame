using System.Text.Json;
using Microsoft.JSInterop;

namespace DFrame.Internal;

internal class LocalStorageAccessor
{
    readonly IJSRuntime jsRuntime;
    readonly JsonSerializerOptions options;

    public LocalStorageAccessor(IJSRuntime jsRuntime)
    {
        this.jsRuntime = jsRuntime;
        this.options = new JsonSerializerOptions
        {
            IncludeFields = true
        };
    }

    public async ValueTask<(bool, T)> TryGetItemAsync<T>(string key, CancellationToken cancellationToken)
    {
        var v = await jsRuntime.InvokeAsync<string>("localStorage.getItem", cancellationToken, key);

        if (v != null)
        {
            return (true, JsonSerializer.Deserialize<T>(v, options)!);
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
        var json = JsonSerializer.Serialize(value, options);
        await jsRuntime.InvokeVoidAsync("localStorage.setItem", cancellationToken, new object[] { key, json });
    }
}