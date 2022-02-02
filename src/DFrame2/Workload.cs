namespace DFrame;

public abstract class Workload
{
    public abstract Task ExecuteAsync(WorkloadContext context);

    public virtual Task SetupAsync(WorkloadContext context)
    {
        return Task.CompletedTask;
    }

    public virtual Task TeardownAsync(WorkloadContext context)
    {
        return Task.CompletedTask;
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