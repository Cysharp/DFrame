using ConsoleAppFramework;
using System.Diagnostics;

await ConsoleApp.RunAsync(args, run);

static async Task run([Argument] int workerCount = 10)
{
    Debug.Assert(workerCount > 0);

    Task[] workerTasks = new Task[workerCount];
    for (int i = 0; i < workerCount; i++)
    {
        workerTasks[i] = ConsoleWorker.Program.Main([]);
    }
    await Task.WhenAll(workerTasks);
}

