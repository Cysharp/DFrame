using DFrame.Core.Collections;
using DFrame.Core.Internal;
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








    public interface IWorkerReceiver
    {
        void CreateCoWorker(int createCount, string typeName);
        void Setup();
        void Execute(int executeCount);
        void Teardown();
        void Shutdown();
    }

    public class WorkerReceiver : IWorkerReceiver
    {
        readonly Channel channel;
        (WorkerContext context, Worker worker)[] coWorkers = default!;

        public WorkerReceiver(Channel channel)
        {
            this.channel = channel;
        }

        public IMasterHub Client { get; set; } = default!;

        public void CreateCoWorker(int createCount, string typeName)
        {
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
                coWorkers[i] = (new WorkerContext(channel), (Worker)coWorker);
            }

            Client.CreateCoWorkerCompleteAsync().Forget();
        }

        public async void Setup()
        {
            await Task.WhenAll(coWorkers.Select(x => x.worker.SetupAsync(x.context)));
            await Client.SetupCompleteAsync();
        }

        public async void Execute(int executeCount)
        {
            await Task.WhenAll(coWorkers.Select(async x =>
            {
                for (int i = 0; i < executeCount; i++)
                {
                    await x.worker.ExecuteAsync(x.context);
                }
            }));
            await Client.ExecuteCompleteAsync();
        }

        public async void Teardown()
        {
            await Task.WhenAll(coWorkers.Select(x => x.worker.TeardownAsync(x.context)));
            await Client.TeardownCompleteAsync();
        }

        public void Shutdown()
        {
            // TODO:???
            throw new NotImplementedException();
        }
    }


    public interface IMasterHub : IStreamingHub<IMasterHub, IWorkerReceiver>
    {
        Task ConnectCompleteAsync();
        Task CreateCoWorkerCompleteAsync();
        Task SetupCompleteAsync();
        Task ExecuteCompleteAsync();
        Task TeardownCompleteAsync();
    }

    public sealed class MasterHub : StreamingHubBase<IMasterHub, IWorkerReceiver>, IMasterHub
    {
        readonly Reporter reporter;

        public MasterHub(Reporter reporter)
        {
            this.reporter = reporter;
        }

        protected override async ValueTask OnConnecting()
        {
            var group = await Group.AddAsync("global-masterhub-group");
            var broadcaster = group.CreateBroadcaster<IWorkerReceiver>();
            reporter.Broadcaster = broadcaster;
        }

        public Task ConnectCompleteAsync()
        {
            reporter.OnConnected.IncrementCount();
            return Task.CompletedTask;
        }

        public Task CreateCoWorkerCompleteAsync()
        {
            reporter.OnCreateCoWorker.IncrementCount();
            return Task.CompletedTask;
        }

        public Task SetupCompleteAsync()
        {
            reporter.OnSetup.IncrementCount();
            return Task.CompletedTask;
        }

        public Task ExecuteCompleteAsync()
        {
            reporter.OnExecute.IncrementCount();
            return Task.CompletedTask;
        }

        public Task TeardownCompleteAsync()
        {
            reporter.OnTeardown.IncrementCount();
            return Task.CompletedTask;
        }
    }
}