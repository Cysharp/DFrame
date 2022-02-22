
using DFrame;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

await Host.CreateDefaultBuilder(args)
    //.ConfigureLogging(x =>
    //{
    //    x.ClearProviders();
    //    x.AddZLoggerConsole();
    //})
    .RunDFrameAsync(new DFrameWorkerOptions("http://localhost:7313")
    {
        VirtualProcess = 32,
        // BatchRate = 50,
        Metadata = new Dictionary<string, string>
        {
            {"MachineName", Environment.MachineName },
            {"ProcessorCount", Environment.ProcessorCount.ToString() },
            {"OSVersion", Environment.OSVersion.ToString() },
        }
    });

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously

public class SampleHttpWorker : Workload
{
    HttpClient httpClient = default!;

    public override async Task SetupAsync(WorkloadContext context)
    {
        httpClient = new HttpClient();
    }

    public override async Task ExecuteAsync(WorkloadContext context)
    {
        await httpClient.GetAsync("http://localhost:5000", context.CancellationToken);
    }

    public override async Task TeardownAsync(WorkloadContext context)
    {
    }
}

public class SampleAppForDIAndParameter : Workload
{
    readonly ILogger<SampleAppForDIAndParameter> logger;
    readonly string message;

    public SampleAppForDIAndParameter(ILogger<SampleAppForDIAndParameter> logger, string message)
    {
        this.logger = logger;
        this.message = message;
    }

    public override async Task ExecuteAsync(WorkloadContext context)
    {
        logger.LogInformation("Execute:" + message);
    }
}