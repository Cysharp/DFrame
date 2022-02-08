﻿// See https://aka.ms/new-console-template for more information
using DFrame;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Text.RegularExpressions;
using ZLogger;
using Microsoft.Extensions.DependencyInjection;

await Host.CreateDefaultBuilder()
    .ConfigureLogging(x =>
    {
        x.ClearProviders();
        x.AddZLoggerConsole();
    })
    .RunDFrameAsync(args, new DFrameWorkerOptions("http://localhost:7313"));

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