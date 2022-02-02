using Cysharp.Diagnostics;
using Zx;

const int ProcessCount = 100;

ConsoleApp.Run(args, async (ConsoleAppContext ctx) =>
{
    await $"cd ../../../../ConsoleApp2/bin/Debug/net6.0/";

    Console.WriteLine("Run ConsoleApp2, Count:" + ProcessCount);
    var tasks = new List<Task>();

    for (int i = 0; i < ProcessCount; i++)
    {
        var t = ProcessX.StartAsync("ConsoleApp2.exe").FirstOrDefaultAsync(ctx.CancellationToken);
        tasks.Add(t);
    }

    await Task.WhenAll(tasks);
});