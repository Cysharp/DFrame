using Cysharp.Diagnostics;
using DFrame;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Text;
using ZLogger;
using Zx;

const int ProcessCount = 30;

Console.OutputEncoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);
await using var commonWriter = new AsyncStreamLineMessageWriter(Console.OpenStandardOutput(), new ZLoggerOptions());

Console.WriteLine("Run Workers virtual process, count:" + ProcessCount);

var tasks = new Task[ProcessCount];
for (int i = 0; i < ProcessCount; i++)
{
    var task = Host.CreateDefaultBuilder()
        .ConfigureLogging(x =>
        {
            x.ClearProviders();
            x.AddZLoggerConsole();
            x.AddZLoggerLogProcessor(commonWriter);
        })
        .RunDFrameAsync(args, new DFrameWorkerOptions("http://localhost:7313"));

    tasks[i] = task;
}

await Task.WhenAll(tasks);

//await Host.CreateDefaultBuilder()
//    .ConfigureLogging(x =>
//    {
//        x.ClearProviders();
//        x.AddZLoggerConsole();
//    })
//    .RunDFrameAsync(args, new DFrameWorkerOptions("http://localhost:7313"));

//ConsoleApp.Run(args, async (ConsoleAppContext ctx) =>
//{
//    ctx.CancellationToken.Register(() =>
//    {
//        Console.WriteLine("Cancellation start.");
//    });

//    await $"cd ../../../../ConsoleApp2/bin/Debug/net6.0/";

//    Console.WriteLine("Run ConsoleApp2, Count:" + ProcessCount);
//    Console.WriteLine("Starting many process in background, If you want to close, should use Ctrl+C before close.");
//    var tasks = new List<Task>();

//    for (int i = 0; i < ProcessCount; i++)
//    {
//        var t = ProcessX.StartAsync("ConsoleApp2.exe")
//            .FirstOrDefaultAsync(ctx.CancellationToken)
//            .ContinueWith(x =>
//            {
//                global::System.Console.WriteLine("Process cancelling.");
//            });
//        tasks.Add(t);
//    }

//    Console.WriteLine("All Process started.");
//    await Task.WhenAll(tasks);
//});


// same as ConsoleApp2?

[Workload("myworkload")]
public class TrialWorkload : Workload
{
    readonly ILogger<TrialWorkload> logger;

    public TrialWorkload(ILogger<TrialWorkload> logger)
    {
        this.logger = logger;
    }

    public override async Task ExecuteAsync(WorkloadContext context)
    {
        logger.LogInformation("Begin:" + context.WorkloadId);
        await Task.Yield();
        await Task.Delay(TimeSpan.FromSeconds(1));
        logger.LogInformation("End:" + context.WorkloadId);
    }
}

[Workload("myworkload2")]
public class TrialWorkload2 : Workload
{
    readonly ILogger<TrialWorkload2> logger;
    readonly int x;
    readonly int y;

    public TrialWorkload2(ILogger<TrialWorkload2> logger, int x, int y)
    {
        this.logger = logger;
        this.x = x;
        this.y = y;
    }

    public override async Task ExecuteAsync(WorkloadContext context)
    {
        logger.LogInformation("Begin:" + context.WorkloadId);
        await Task.Yield();
        await Task.Delay(TimeSpan.FromSeconds(1));
        logger.LogInformation("End:" + context.WorkloadId);
    }
}