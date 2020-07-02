using DFrame.Core.Collections;
using Grpc.Core;
using MagicOnion;
using MagicOnion.Client;
using MagicOnion.Server;
using MagicOnion.Server.Hubs;
using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace DFrame.Core
{
    // TODO:???
    public interface IMaster
    {
        Task MasterSetupAsync();
        Task MasterShutdownAsync();
    }

    public class WorkerContext
    {
        readonly Channel masterChannel;
        public string WorkerId { get; }

        public WorkerContext(Channel masterChannel)
        {
            this.masterChannel = masterChannel;
            this.WorkerId = Guid.NewGuid().ToString();
        }

        public IDistributedQueue<T> CreateDistributedQueue<T>()
        {
            var typeKey = this.GetType().FullName;
            var client = MagicOnionClient.Create<IDistributedQueueService>(masterChannel, new IClientFilter[] { new AddHeaderFilter("queue-key", typeKey) });
            return new DistributedQueue<T>(client);
        }
    }

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

    public interface INoneReceiver
    {
    }

    public class NoneReceiver : INoneReceiver
    {
        public static readonly INoneReceiver Instance = new NoneReceiver();

        NoneReceiver()
        {

        }
    }

    public interface IWorkerHub : IStreamingHub<IWorkerHub, INoneReceiver>
    {
        Task CreateCoWorkerAsync(int createCount, string typeName, string masterTarget);
        Task SetupAsync();
        Task ExecuteAsync(int executeCount);
        Task TeardownAsync();
        Task ShutdownAsync();
    }

    public sealed class WorkerHub : StreamingHubBase<IWorkerHub, INoneReceiver>, IWorkerHub
    {
#pragma warning disable CS8618 // Non-nullable field is uninitialized. Consider declaring as nullable.
        (WorkerContext context, Worker worker)[] coWorkers;
        IGroup group;
#pragma warning restore CS8618

        protected override async ValueTask OnConnecting()
        {
            group = await Group.AddAsync(Guid.NewGuid().ToString());
        }

        public Task CreateCoWorkerAsync(int createCount, string typeName, string masterTarget)
        {
            var masterChannel = new Channel(masterTarget, ChannelCredentials.Insecure);

            // TODO:Entry?
            var type = Assembly.GetEntryAssembly().GetType(typeName);

            this.coWorkers = new (WorkerContext, Worker)[createCount];
            for (int i = 0; i < coWorkers.Length; i++)
            {
                // TODO: ExpressionTree Lambda
                // register to DI.
                //var coWorker = typeof(IServiceLocator).GetMethod("GetService").MakeGenericMethod(type)
                //    .Invoke(this.Context.ServiceLocator, null);
                var coWorker = Activator.CreateInstance(type);
                coWorkers[i] = (new WorkerContext(masterChannel), (Worker)coWorker);
            }

            return Task.CompletedTask;
        }

        public async Task SetupAsync()
        {
            await Task.WhenAll(coWorkers.Select(x => x.worker.SetupAsync(x.context)));
        }

        public async Task ExecuteAsync(int executeCount)
        {
            await Task.WhenAll(coWorkers.Select(async x =>
            {
                for (int i = 0; i < executeCount; i++)
                {
                    await x.worker.ExecuteAsync(x.context);
                }
            }));
        }

        public async Task TeardownAsync()
        {
            await Task.WhenAll(coWorkers.Select(x => x.worker.TeardownAsync(x.context)));
        }


        public Task ShutdownAsync()
        {
            // exit after???
            _ = Task.Delay(TimeSpan.FromSeconds(1)).ContinueWith(_ => Environment.Exit(0));

            return Task.CompletedTask;
        }
    }
}