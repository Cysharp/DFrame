using MagicOnion;
using MagicOnion.Server.Hubs;
using System;
using System.Threading.Tasks;

namespace DFrame
{
    public interface IMasterHub : IStreamingHub<IMasterHub, IWorkerReceiver>
    {
        Task ConnectCompleteAsync(Guid nodeId);
        Task CreateCoWorkerCompleteAsync();
        Task SetupCompleteAsync();
        Task ExecuteCompleteAsync(ExecuteResult[] result);
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

        protected override ValueTask OnDisconnected()
        {
            return base.OnDisconnected();
        }

        public Task ConnectCompleteAsync(Guid nodeId)
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

        public Task ExecuteCompleteAsync(ExecuteResult[] result)
        {
            reporter.AddExecuteResult(result);
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