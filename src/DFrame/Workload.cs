using System;
using System.Threading;
using System.Threading.Tasks;

namespace DFrame
{
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

    public abstract class Workload<TMaster> : Workload
        where TMaster : Master
    {

    }

    public abstract class Master
    {
        public abstract Task SetupAsync(CancellationToken cancellationToken);
        public abstract Task TeardownAsync(CancellationToken cancellationToken);
    }

    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public class WorkloadAttribute : Attribute
    {
        public string Name { get; }
        public bool DisallowSingleExecute { get; }

        public WorkloadAttribute(string name, bool disallowSingleExecute = false)
        {
            Name = name;
            DisallowSingleExecute = disallowSingleExecute;
        }
    }
}