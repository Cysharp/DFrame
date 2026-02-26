using DFrame;

DFrameApp.Run(7312, 7313); // WebUI:7312, WorkerListen:7313

public class SampleWorkload : Workload
{
    public override async Task ExecuteAsync(WorkloadContext context)
    {
        Console.WriteLine($"Hello {context.WorkloadId}");
    }
}
