// See https://aka.ms/new-console-template for more information
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

