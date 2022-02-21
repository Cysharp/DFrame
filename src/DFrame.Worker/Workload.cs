namespace DFrame;

public abstract class Workload
{
    int isDisposed = 0;

    public abstract Task ExecuteAsync(WorkloadContext context);

    public virtual Task SetupAsync(WorkloadContext context)
    {
        return Task.CompletedTask;
    }

    public virtual Task TeardownAsync(WorkloadContext context)
    {
        return Task.CompletedTask;
    }

    internal async Task InternalTeardownAsync(WorkloadContext context)
    {
        if (Interlocked.Increment(ref isDisposed) == 1)
        {
            await TeardownAsync(context);
        }
    }
}

[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public class WorkloadAttribute : Attribute
{
    public string Name { get; }

    public WorkloadAttribute(string name)
    {
        Name = name;
    }
}