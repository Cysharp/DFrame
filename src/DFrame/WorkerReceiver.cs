using DFrame.Internal;
using Grpc.Core;
using Grpc.Net.Client;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace DFrame
{
    public interface IWorkerReceiver
    {
        void CreateWorkloadAndSetup(int createCount, string workloadName);
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
        readonly Guid workerId;
        readonly DFrameWorkloadCollection workloadCollection;
        readonly DFrameOptions options;
        readonly IServiceProvider serviceProvider;
        readonly TaskCompletionSource<object?> receiveShutdown;
        readonly ILogger logger;
        ImmutableArray<(WorkloadContext context, Workload workload)> workloads;

        internal WorkerReceiver(GrpcChannel channel, Guid workerId, IServiceProvider serviceProvider, DFrameOptions options)
        {
            // this.logger = logger;
            this.channel = channel;
            this.workerId = workerId;
            this.workloadCollection = (DFrameWorkloadCollection)serviceProvider.GetRequiredService(typeof(DFrameWorkloadCollection));
            this.serviceProvider = serviceProvider;
            this.options = options;
            this.logger = serviceProvider.GetRequiredService<ILogger<WorkerReceiver>>();
            this.receiveShutdown = new TaskCompletionSource<object?>(TaskCreationOptions.RunContinuationsAsynchronously);
            this.workloads = ImmutableArray<(WorkloadContext context, Workload worker)>.Empty;
        }

        public IMasterHub Client { get; set; } = default!;

        public Task WaitShutdown => receiveShutdown.Task;

        public async void CreateWorkloadAndSetup(int createCount, string workloadName)
        {
            logger.LogInformation($"Creating {createCount} workload(s) of '{workloadName}'");
            ThreadPoolUtility.SetMinThread(createCount);
            if (!workloadCollection.TryGetWorkload(workloadName, out var description))
            {
                throw new InvalidOperationException($"Workload:{workloadName} does not found in assembly.");
            }

            var requireSetupWorkloads = new List<(WorkloadContext context, Workload workload)>(createCount);
            var newWorkloads = workloads.ToBuilder();
            for (int i = 0; i < createCount; i++)
            {
                var workload = serviceProvider.GetRequiredService(description.WorkloadType);
                var t = (new WorkloadContext(channel, options), (Workload)workload);
                newWorkloads.Add(t);
                requireSetupWorkloads.Add(t);
            }

            await Task.WhenAll(requireSetupWorkloads.Select(x => x.workload.SetupAsync(x.context)));

            workloads = newWorkloads.ToImmutable();
            await Client.CreateWorkloadCompleteAsync();
        }

        public async void Execute(int executeCount)
        {
            logger.LogInformation($"Executing {workloads.Length} workload(s). (ExecutePerWorkload={executeCount})");
            // TODO:add progress...
            //var progress = workloads.Length * executeCount / 10;
            //var increment = 0;

            var result = await Task.WhenAll(workloads.Select(x => Task.Run(async () =>
            {
                var list = new List<ExecuteResult>(executeCount);
                for (int i = 0; i < executeCount; i++)
                {
                    string? errorMsg = null;
                    var sw = ValueStopwatch.StartNew();
                    try
                    {
                        await x.workload.ExecuteAsync(x.context);
                    }
                    catch (Exception ex)
                    {
                        errorMsg = ex.ToString();
                    }

                    var executeResult = new ExecuteResult(x.context.WorkloadId, sw.Elapsed, i, (errorMsg != null), errorMsg);
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
            await Task.WhenAll(workloads.Select(x => x.workload.TeardownAsync(x.context)));
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
            logger.LogInformation($"Executing workload(s) until a stop request is received.");
            while (!receiveStopped)
            {
                await Task.WhenAll(workloads.Select(x => Task.Run(async () =>
                {
                    string? errorMsg = null;
                    var sw = ValueStopwatch.StartNew();
                    try
                    {
                        await x.workload.ExecuteAsync(x.context);
                    }
                    catch (Exception ex)
                    {
                        errorMsg = ex.ToString();
                    }

                    var executeResult = new ExecuteResult(x.context.WorkloadId, sw.Elapsed, 0, (errorMsg != null), errorMsg);
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