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
