using System;
using System.Threading.Tasks;

namespace DFrame
{
    public abstract class Worker
    {
        // public Dis Create
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
    {

    }

    public abstract class Master
    {
        public abstract Task SetupAsync();
        public abstract Task TeardownAsync();
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

    public class GroupAttribute : Attribute
    {
        public string GroupName { get; }
        public int Order { get; }

        public GroupAttribute(string groupName, int order)
        {
            GroupName = groupName;
            Order = order;
        }
    }
}