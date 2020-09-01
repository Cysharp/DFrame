using DFrame.Internal;
using MagicOnion;
using MagicOnion.Server.Hubs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace DFrame
{
    public interface IMasterHub : IStreamingHub<IMasterHub, IWorkerReceiver>
    {
        Task CreateCoWorkerCompleteAsync();
        Task ReportProgressAsync(ExecuteResult result);
        Task ExecuteCompleteAsync(ExecuteResult[] result);
        Task TeardownCompleteAsync();
    }

    public sealed class MasterHub : StreamingHubBase<IMasterHub, IWorkerReceiver>, IMasterHub
    {
        readonly WorkerConnectionGroupContext workerConnectionContext;
        Guid nodeId;

        public MasterHub(WorkerConnectionGroupContext connectionContext)
        {
            this.workerConnectionContext = connectionContext;
        }

        protected override async ValueTask OnConnecting()
        {
            nodeId = Guid.Parse(Context.CallContext.RequestHeaders.GetValue("node-id"));

            // TODO:use specified id???
            var group = await Group.AddAsync("global-masterhub-group");
            var broadcaster = group.CreateBroadcaster<IWorkerReceiver>();
            workerConnectionContext.Broadcaster = broadcaster;

            workerConnectionContext.AddConnection(nodeId);
        }

        protected override ValueTask OnDisconnected()
        {
            workerConnectionContext.RemoveConnection(nodeId);
            return default;
        }

        public Task CreateCoWorkerCompleteAsync()
        {
            workerConnectionContext.OnCreateCoWorkerAndSetup.Done(nodeId);
            return Task.CompletedTask;
        }

        public Task ReportProgressAsync(ExecuteResult result)
        {
            // TODO: throw new NotImplementedException();
            return Task.CompletedTask;
        }

        public Task ExecuteCompleteAsync(ExecuteResult[] result)
        {
            // TODO:
            // workerConnectionContext.AddExecuteResult(result);
            workerConnectionContext.OnExecute.Done(nodeId);
            return Task.CompletedTask;
        }

        public Task TeardownCompleteAsync()
        {
            workerConnectionContext.OnTeardown.Done(nodeId);
            return Task.CompletedTask;
        }
    }

    public class WorkerConnectionGroupContext
    {
        int maxProcessCount;
        bool throwErrorOnRemoved;
        HashSet<Guid> connections = default!;
        TaskCompletionSource<object?> allConnectionConnectComplete = default!;
        List<ExecuteResult> executeResult = default!;
        public IReadOnlyList<ExecuteResult> ExecuteResult => executeResult;

        public IWorkerReceiver Broadcaster { get; internal set; } = default!;

        public CountingSource OnConnected { get; private set; } = default!;
        public CountingSource OnCreateCoWorkerAndSetup { get; private set; } = default!;
        public CountingSource OnExecute { get; private set; } = default!;
        public CountingSource OnTeardown { get; private set; } = default!;

        // TODO: reset???
        public void Initialize(int processCount, bool throwErrorOnRemoved)
        {
            this.connections = new HashSet<Guid>();
            this.allConnectionConnectComplete = new TaskCompletionSource<object?>(TaskCreationOptions.RunContinuationsAsynchronously);
            this.maxProcessCount = processCount;
            this.executeResult = new List<ExecuteResult>();
            this.throwErrorOnRemoved = throwErrorOnRemoved;
        }

        public Task WaitAllConnectedWithTimeoutAsync(TimeSpan timeout, CancellationToken cancellationToken, Task taskSignal)
        {
            return allConnectionConnectComplete.Task.WithTimeoutAndCancellationAndTaskSignal(timeout, cancellationToken, taskSignal);
        }

        public void AddExecuteResult(ExecuteResult[] results)
        {
            lock (executeResult)
            {
                executeResult.AddRange(results);
            }
        }

        public void AddConnection(Guid guid)
        {
            lock (connections)
            {
                connections.Add(guid);
                SignalWhenReachedAllWorkerIsConnected();
            }
        }

        public void RemoveConnection(Guid guid)
        {
            lock (connections)
            {
                // already connected, decr.
                if (connections.Remove(guid))
                {
                    maxProcessCount--;
                }

                if (throwErrorOnRemoved)
                {
                    allConnectionConnectComplete.TrySetException(new ConnectionDisconnectedException(guid));
                }

                SignalWhenReachedAllWorkerIsConnected();
            }
        }

        void SignalWhenReachedAllWorkerIsConnected()
        {
            if (connections.Count == maxProcessCount)
            {
                var connectionIds = connections.ToArray();
                this.OnConnected = new CountingSource(connectionIds, throwErrorOnRemoved);
                this.OnCreateCoWorkerAndSetup = new CountingSource(connectionIds, throwErrorOnRemoved);
                this.OnExecute = new CountingSource(connectionIds, throwErrorOnRemoved);
                this.OnTeardown = new CountingSource(connectionIds, throwErrorOnRemoved);

                allConnectionConnectComplete.TrySetResult(null);
            }
        }
    }

    public class CountingSource
    {
        readonly Dictionary<Guid, bool> doneStatus; // Use roaring bitmap gets better performance...
        readonly bool throwErrorOnRemoved;
        TaskCompletionSource<object?> waiter;

        public CountingSource(Guid[] connectionIds, bool throwErrorOnRemoved)
        {
            this.doneStatus = new Dictionary<Guid, bool>();
            this.throwErrorOnRemoved = throwErrorOnRemoved;
            foreach (var item in connectionIds)
            {
                doneStatus.Add(item, false);
            }
            this.waiter = new TaskCompletionSource<object?>(TaskCreationOptions.RunContinuationsAsynchronously);
        }

        public void Reset()
        {
            foreach (var key in doneStatus.Keys.ToArray())
            {
                doneStatus[key] = false;
            }
            this.waiter = new TaskCompletionSource<object?>(TaskCreationOptions.RunContinuationsAsynchronously);
        }

        public Task WaitWithTimeoutAsync(TimeSpan timeout, CancellationToken cancellationToken, Task taskSignal)
        {
            return waiter.Task.WithTimeoutAndCancellationAndTaskSignal(timeout, cancellationToken, taskSignal);
        }

        public void Done(Guid guid)
        {
            lock (doneStatus)
            {
                doneStatus[guid] = true;
                SignalWhenAllFlagIsTrue();
            }
        }

        public void Remove(Guid guid)
        {
            lock (doneStatus)
            {
                doneStatus.Remove(guid);
                if (throwErrorOnRemoved)
                {
                    waiter.TrySetException(new ConnectionDisconnectedException(guid));
                }
                else
                {
                    SignalWhenAllFlagIsTrue();
                }
            }
        }

        void SignalWhenAllFlagIsTrue()
        {
            foreach (var item in doneStatus)
            {
                if (!item.Value) return;
            }

            waiter.TrySetResult(null);
        }
    }

    internal class ConnectionDisconnectedException : Exception
    {
        public Guid NodeId { get; }

        public ConnectionDisconnectedException(Guid nodeId)
        {
            NodeId = nodeId;
        }
    }
}