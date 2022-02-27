#pragma warning disable CS1998

using DFrame;
using Microsoft.Extensions.DependencyInjection;
using System.Buffers;

var builder = DFrameApp.CreateBuilder(7312, 7313);
builder.ConfigureWorker(options =>
{
    options.VirtualProcess = 32;
    options.Metadata = new()
    {
        { "MachineName", Environment.MachineName },
        { "ProcessorCount", Environment.ProcessorCount.ToString() }
    };
});
builder.Run();


builder.ConfigureServices(services =>
{
    services.AddSingleton<HttpClient>();
});
await builder.RunAsync();


public class Http1GetEcho : Workload
{
    protected static readonly HttpClient DefaultHttpClient = new HttpClient(new SocketsHttpHandler
    {
        MaxConnectionsPerServer = int.MaxValue,
        AutomaticDecompression = System.Net.DecompressionMethods.None,
    });

    protected static async Task ReadToEndAsync(Stream source, CancellationToken cancellationToken)
    {
        byte[] buffer = ArrayPool<byte>.Shared.Rent(65536);
        try
        {
            int bytesRead;
            while ((bytesRead = await source.ReadAsync(new Memory<byte>(buffer), cancellationToken).ConfigureAwait(false)) != 0)
            {
            }
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }
    }

    protected static async Task ReadResponseAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        var stream = await response.EnsureSuccessStatusCode().Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
        await ReadToEndAsync(stream, cancellationToken).ConfigureAwait(false);
    }

    public override async Task ExecuteAsync(WorkloadContext context)
    {
        var r = await DefaultHttpClient.GetAsync("http://localhost:5111");
        await ReadToEndAsync(await r.EnsureSuccessStatusCode().Content.ReadAsStreamAsync(), context.CancellationToken);
    }
}
public class ReturnResult : Workload
{
    DateTime beginTime;
    DateTime endTime;
    int executeCount;

    public override async Task SetupAsync(WorkloadContext context)
    {
        beginTime = DateTime.Now;
    }

    public override async Task ExecuteAsync(WorkloadContext context)
    {
        endTime = DateTime.Now;
        executeCount++;
    }

    public override Dictionary<string, string>? Complete(WorkloadContext context)
    {
        return new()
        {
            { "begin", beginTime.ToString() },
            { "end", endTime.ToString() },
            { "count", executeCount.ToString() },
        };
    }
}