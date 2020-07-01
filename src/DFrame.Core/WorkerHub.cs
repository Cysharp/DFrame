using DFrame.Core.Collections;
using Grpc.Core;
using MagicOnion;
using MagicOnion.Client;
using MagicOnion.Server.Hubs;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace DFrame.Core
{
    // TODO:???
    public interface IMaster
    {
        Task MasterSetupAsync();
        Task MasterShutdownAsync();
    }

    public interface IWorker
    {
        Task SetupAsync();
        Task ExecuteAsync();
        Task TeardownAsync();
    }

    public class WorkerContext
    {
        public WorkerContext()
        {
            // to master Channel????
        }

        public IDistributedQueue<T> CreateDistributedQueue<T>()
        {
            var typeKey = this.GetType().FullName;
            var q = MagicOnionClient.Create<IDistributedQueue<T>>((Channel)null, new IClientFilter[] { new AddHeaderFilter("queue-key", typeKey) });
            return q;
        }
    }

    public class GroupContext
    {

    }



    public abstract class WorkerBase : IWorker
    {
        // public Dis Create
        public abstract Task ExecuteAsync();

        public virtual Task SetupAsync()
        {
            return Task.CompletedTask;
        }

        public virtual Task TeardownAsync()
        {
            return Task.CompletedTask;
        }
    }

    public class AddHeaderFilter : IClientFilter
    {
        readonly string key;
        readonly string value;

        public AddHeaderFilter(string key, string value)
        {
            this.key = key;
            this.value = value;
        }

        public ValueTask<ResponseContext> SendAsync(RequestContext context, Func<RequestContext, ValueTask<ResponseContext>> next)
        {
            context.CallOptions.Headers.Add(key, value);
            return next(context);
        }
    }

    // TODO: for Worker -> Master event.
    public interface INoneReceiver
    {
    }

    public interface IWorkerHub : IStreamingHub<IWorkerHub, INoneReceiver>
    {
        Task CreateCoWorkerAsync(int count, string typeName);
        Task SetupAsync();
        Task ExecuteAsync();
        Task TeardownAsync();
        Task ShutdownAsync();
    }

    public sealed class WorkerHub : StreamingHubBase<IWorkerHub, INoneReceiver>, IWorkerHub
    {
        IWorker[] coWorkers;

        IGroup group;

        protected override async ValueTask OnConnecting()
        {
            group = await Group.AddAsync(Guid.NewGuid().ToString());
        }

        public Task CreateCoWorkerAsync(int count, string typeName)
        {
            // TODO:GetService(Type.GetType(typeName));
            this.coWorkers = new IWorker[count];
            for (int i = 0; i < coWorkers.Length; i++)
            {
                coWorkers[i] = this.Context.ServiceLocator.GetService<IWorker>();
            }



            // Broadcast(group).EnqueueAsync(



            return Task.CompletedTask;
        }

        public async Task SetupAsync()
        {
            await Task.WhenAll(coWorkers.Select(x => x.SetupAsync()));
        }

        public async Task ExecuteAsync()
        {
            await Task.WhenAll(coWorkers.Select(x => x.ExecuteAsync()));
        }

        public async Task TeardownAsync()
        {
            await Task.WhenAll(coWorkers.Select(x => x.TeardownAsync()));
        }


        public Task ShutdownAsync()
        {
            // exit after???
            _ = Task.Delay(TimeSpan.FromSeconds(1)).ContinueWith(_ => Environment.Exit(0));

            return Task.CompletedTask;
        }
    }
}