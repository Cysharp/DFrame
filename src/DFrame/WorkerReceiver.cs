using DFrame.Internal;
using Grpc.Core;
using Grpc.Net.Client;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;

namespace DFrame
{
    public interface IWorkerReceiver
    {
        void CreateCoWorkerAndSetup(int createCount, string workerName);
        void Execute(int executeCount);
        void ExecuteUntilReceiveStop();
        void Stop();
        void Teardown();
        void Shutdown();
    }

    internal class WorkerReceiver : IWorkerReceiver
    {
        // readonly ILogger<WorkerReceiver> logger;
        readonly GrpcChannel channel;
        readonly Guid nodeId;
        readonly DFrameWorkerCollection workerCollection;
        readonly DFrameOptions options;
        readonly IServiceProvider serviceProvider;
        readonly TaskCompletionSource<object?> receiveShutdown;
        ImmutableArray<(WorkerContext context, Worker worker)> coWorkers;

        internal WorkerReceiver(GrpcChannel channel, Guid nodeId, IServiceProvider serviceProvider, DFrameOptions options)
        {
            // this.logger = logger;
            this.channel = channel;
            this.nodeId = nodeId;
            this.workerCollection = (DFrameWorkerCollection)serviceProvider.GetService(typeof(DFrameWorkerCollection));
            this.serviceProvider = serviceProvider;
            this.options = options;
            this.receiveShutdown = new TaskCompletionSource<object?>(TaskCreationOptions.RunContinuationsAsynchronously);
            this.coWorkers = ImmutableArray<(WorkerContext context, Worker worker)>.Empty;
        }

        public IMasterHub Client { get; set; } = default!;

        public Task WaitShutdown => receiveShutdown.Task;

        public async void CreateCoWorkerAndSetup(int createCount, string workerName)
        {
            ThreadPoolUtility.SetMinThread(createCount);
            if (!workerCollection.TryGetWorker(workerName, out var description))
            {
                throw new InvalidOperationException($"Worker:{workerName} does not found in assembly.");
            }

            var requireSetupCoWorkers = new List<(WorkerContext context, Worker worker)>(createCount);
            var newCoWorkers = coWorkers.ToBuilder();
            for (int i = 0; i < createCount; i++)
            {
                var coWorker = serviceProvider.GetService(description.WorkerType);
                var t = (new WorkerContext(channel, options), (Worker)coWorker);
                newCoWorkers.Add(t);
                requireSetupCoWorkers.Add(t);
            }

            await Task.WhenAll(requireSetupCoWorkers.Select(x => x.worker.SetupAsync(x.context)));

            coWorkers = newCoWorkers.ToImmutable();
            await Client.CreateCoWorkerCompleteAsync();
        }

        public async void Execute(int executeCount)
        {
            // TODO:add progress...
            //var progress = coWorkers.Length * executeCount / 10;
            //var increment = 0;

            var result = await Task.WhenAll(coWorkers.Select(x => Task.Run(async () =>
            {
                var list = new List<ExecuteResult>(executeCount);
                for (int i = 0; i < executeCount; i++)
                {
                    string? errorMsg = null;
                    var sw = ValueStopwatch.StartNew();
                    try
                    {
                        await x.worker.ExecuteAsync(x.context);
                    }
                    catch (Exception ex)
                    {
                        errorMsg = ex.ToString();
                    }

                    var executeResult = new ExecuteResult(x.context.WorkerId, sw.Elapsed, i, (errorMsg != null), errorMsg);
                    list.Add(executeResult);

                    // TODO:toriaezu
                    _ = Task.Run(() => Client.ReportProgressAsync(executeResult));
                }
                return list;
            })));

            await Client.ExecuteCompleteAsync(result.SelectMany(xs => xs).ToArray());
        }

        public async void Teardown()
        {
            await Task.WhenAll(coWorkers.Select(x => x.worker.TeardownAsync(x.context)));
            await Client.TeardownCompleteAsync();
        }

        public void Shutdown()
        {
            receiveShutdown.TrySetResult(null);
        }

        // for Ramp-up

        bool receiveStopped;

        public async void ExecuteUntilReceiveStop()
        {
            while (!receiveStopped)
            {
                await Task.WhenAll(coWorkers.Select(x => Task.Run(async () =>
                {
                    string? errorMsg = null;
                    var sw = ValueStopwatch.StartNew();
                    try
                    {
                        await x.worker.ExecuteAsync(x.context);
                    }
                    catch (Exception ex)
                    {
                        errorMsg = ex.ToString();
                    }

                    var executeResult = new ExecuteResult(x.context.WorkerId, sw.Elapsed, 0, (errorMsg != null), errorMsg);
                    // TODO:toriaezu
                    _ = Task.Run(() => Client.ReportProgressAsync(executeResult));
                })));
            }

            await Client.ExecuteCompleteAsync(new ExecuteResult[0]); // TODO:use others.
        }

        public void Stop()
        {
            receiveStopped = true;
        }
    }
}