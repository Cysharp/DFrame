using Cysharp.Diagnostics;
using Zx;

const int ProcessCount = 30;

ConsoleApp.Run(args, async (ConsoleAppContext ctx) =>
{
    ctx.CancellationToken.Register(() =>
    {
        Console.WriteLine("Cancellation start.");
    });

    await $"cd ../../../../ConsoleApp2/bin/Debug/net6.0/";

    Console.WriteLine("Run ConsoleApp2, Count:" + ProcessCount);
    Console.WriteLine("Starting many process in background, If you want to close, should use Ctrl+C before close.");
    var tasks = new List<Task>();

    for (int i = 0; i < ProcessCount; i++)
    {
        var t = ProcessX.StartAsync("ConsoleApp2.exe")
            .FirstOrDefaultAsync(ctx.CancellationToken)
            .ContinueWith(x =>
            {
                global::System.Console.WriteLine("Process cancelling.");
            });
        tasks.Add(t);
    }

    Console.WriteLine("All Process started.");
    await Task.WhenAll(tasks);
});