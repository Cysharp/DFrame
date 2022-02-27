using DFrame;
using Microsoft.Extensions.DependencyInjection;


var builder = DFrameApp.CreateBuilder(7312, 7313);
builder.ConfigureWorker(options =>
{
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