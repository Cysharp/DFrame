using DFrame;

DFrameApp.Run("http://localhost:7312");

#pragma warning disable CS1998
public class SampleWorkload : Workload
{
    public override async Task ExecuteAsync(WorkloadContext context)
    {
        Console.WriteLine($"{context.WorkloadId} Hello!");
    }
}

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