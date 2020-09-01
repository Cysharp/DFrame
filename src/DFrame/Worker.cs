using System;
using System.Threading;
using System.Threading.Tasks;

namespace DFrame
{
    public abstract class Worker
    {
        public abstract Task ExecuteAsync(WorkerContext context);

        public virtual Task SetupAsync(WorkerContext context)
        {
            return Task.CompletedTask;
        }

        public virtual Task TeardownAsync(WorkerContext context)
        {
            return Task.CompletedTask;
        }
    }

    public abstract class Worker<TMaster> : Worker
        where TMaster : Master
    {

    }

    public abstract class Master
    {
        public abstract Task SetupAsync(CancellationToken cancellationToken);
        public abstract Task TeardownAsync(CancellationToken cancellationToken);
    }

    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public class WorkerAttribute : Attribute
    {
        public string Name { get; }
        public bool DisallowSingleExecute { get; }

        public WorkerAttribute(string name, bool disallowSingleExecute = false)
        {
            Name = name;
            DisallowSingleExecute = disallowSingleExecute;
        }
    }
}