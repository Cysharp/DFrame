using DFrame;
using DFrame.Controller;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using System.Text.Json.Serialization;

var builder = DFrameApp.CreateBuilder("http://localhost:7312");
builder.ConfigureServices(services =>
{
    // services.AddSingleton<IExecutionResultHistoryProvider>(new FlatFileLogExecutionResultHistoryProvider("results"));
});
builder.ConfigureWorker(options =>
{
    options.IncludesDefaultHttpWorkload = true;
    options.Metadata = new Dictionary<string, string>
    {
        {"ProcessorCount", Environment.ProcessorCount.ToString() }
    };
});

builder.Run();
//DFrameApp.Run("http://localhost:7312");




#pragma warning disable CS1998
public class SampleWorkload : Workload
{
    readonly string world;
    int i;

    public SampleWorkload(string world)
    {
        this.world = world;
    }

    public override async Task ExecuteAsync(WorkloadContext context)
    {
        Interlocked.Increment(ref i);
    }

    public override Dictionary<string, string>? Complete()
    {
        return new Dictionary<string, string>
        {
            { "succeed", i.ToString() },
            { "hogemoge", "takoyaki!" }
        };
    }
}



public class FlatFileLogExecutionResultHistoryProvider : IExecutionResultHistoryProvider
{
    readonly string rootDir;
    readonly IExecutionResultHistoryProvider memoryProvider;

    public event Action? NotifyCountChanged;

    public FlatFileLogExecutionResultHistoryProvider(string rootDir)
    {
        this.rootDir = rootDir;
        this.memoryProvider = new InMemoryExecutionResultHistoryProvider();
    }

    public int GetCount()
    {
        return memoryProvider.GetCount();
    }

    public IReadOnlyList<ExecutionSummary> GetList()
    {
        return memoryProvider.GetList();
    }

    public (ExecutionSummary Summary, SummarizedExecutionResult[] Results)? GetResult(DFrame.Controller.ExecutionId executionId)
    {
        return memoryProvider.GetResult(executionId);
    }

    public void AddNewResult(ExecutionSummary summary, SummarizedExecutionResult[] results)
    {
        var fileName = $"{summary.StartTime.ToString("yyyy-MM-dd hh.mm.ss")} {summary.Workload} {summary.ExecutionId}";
        var json = JsonSerializer.Serialize(new { summary, results }, new JsonSerializerOptions { WriteIndented = true });

        var d = Directory.CreateDirectory(rootDir);
        Console.WriteLine(d.FullName);
        File.WriteAllText(Path.Combine(rootDir, fileName), json);

        var hogehoge = JsonSerializer.Deserialize<LogFor>(json);
        var json2 = JsonSerializer.Serialize(hogehoge, new JsonSerializerOptions { WriteIndented = true });

        memoryProvider.AddNewResult(summary, results);
        NotifyCountChanged?.Invoke();
    }
}

public record LogFor(ExecutionSummary summary, SummarizedExecutionResult[] results);

//using DFrame;
//using Microsoft.Extensions.Logging;
//using ZLogger;

//// DFrameApp.Run("http://localhost:7312");

//var builder = DFrameApp.CreateBuilder("http://localhost:7312");
//builder.ConfigureLogging(x =>
//{
//    x.ClearProviders();
//    x.AddZLoggerConsole();
//});
//builder.ConfigureWorker((ctx, options) =>
//{
//    options.BatchRate = 1000;
//    // options.VirtualProcess = 32;
//});

//await builder.RunAsync();

//#pragma warning disable CS1998
//public class SampleWorkload : Workload
//{
//    public override async Task ExecuteAsync(WorkloadContext context)
//    {
//        Console.WriteLine($"{context.WorkloadId} Hello!");
//    }
//}

//public class LoggerWorkload : Workload
//{
//    readonly ILogger<LoggerWorkload> logger;

//    public LoggerWorkload(ILogger<LoggerWorkload> logger)
//    {
//        this.logger = logger;
//    }

//    public override async Task ExecuteAsync(WorkloadContext context)
//    {
//        logger.ZLogInformation("{0} Hello!", context.WorkloadId);
//    }
//}