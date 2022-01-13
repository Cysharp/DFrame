using DFrame.Collections;
using DFrame.Internal;
using Grpc.Core;
using MagicOnion.Server;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Net;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Threading.Tasks;
using ZLogger;

namespace DFrame
{
    internal abstract class DFrameRunnerBase
    {
        protected ILogger<DFrameApp> logger;
        protected IServiceProvider provider;
        protected DFrameOptions options;
        protected DFrameWorkloadCollection workloads;
        protected IHost? masterHost;

        public DFrameRunnerBase(ILogger<DFrameApp> logger, IServiceProvider provider, DFrameOptions options, DFrameWorkloadCollection workloads)
        {
            this.provider = provider;
            this.logger = logger;
            this.workloads = workloads;
            this.options = options;
        }

        protected abstract Task CreateWorkloadAndExecuteAsync(IWorkerReceiver broadcaster, WorkerConnectionGroupContext workerConnection, string workloadName, CancellationToken cancellationToken, TaskFailSignal failSignal);

        public async Task RunAsync(
            string workloadName,
            int workerCount,
            int maxWorkloadPerWorker, // TODO: remove it
            int executePerWorkload, // TODO: remove it
            ConsoleAppFramework.ConsoleAppContext consoleAppContext) // TODO:remove this context.
        {
            var cancellationToken = consoleAppContext.CancellationToken;
            var args = consoleAppContext.Arguments;

            ThreadPoolUtility.SetMinThread(maxWorkloadPerWorker);
            // validate workload is exists.
            if (!workloads.TryGetWorkload(workloadName, out var workloadInfo))
            {
                throw new InvalidOperationException($"Workload:{workloadName} does not found in assembly.");
            }

            var failSignal = new TaskFailSignal();

            using (masterHost = StartMasterHost(args, cancellationToken))
            await using (options.ScalingProvider)
            {
                var workerConnection = masterHost.Services.GetRequiredService<WorkerConnectionGroupContext>();
                workerConnection.Initialize(workerCount, options.WorkerDisconnectedBehaviour == WorkerDisconnectedBehaviour.Stop);

                logger.LogInformation($"Runner '{this.GetType().Name}' starts {workerCount} worker(s) using '{options.ScalingProvider.GetType().Name}'.");
                await options.ScalingProvider.StartWorkerAsync(options, workerCount, provider, failSignal, cancellationToken).WithTimeoutAndCancellationAndTaskSignal(options.Timeout, cancellationToken, failSignal.Task);

                // wait worker is connected
                logger.LogInformation($"Waiting for workers to be ready.");
                await workerConnection.WaitAllConnectedWithTimeoutAsync(options.Timeout, cancellationToken, failSignal.Task);
                logger.LogInformation($"Workers are ready.");

                var broadcaster = workerConnection.Broadcaster;

                Master? master = default;
                if (workloadInfo.MasterType != null)
                {
                    master = provider.GetRequiredService(workloadInfo.MasterType) as Master;
                }

                // MasterSetup
                if (master != null)
                {
                    logger.LogTrace("Invoke MasterSetup");
                    using (var linkedToken = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken))
                    {
                        linkedToken.CancelAfter(options.Timeout);
                        await master.SetupAsync(linkedToken.Token);
                    }
                }

                logger.LogInformation($"Create and execute workload '{workloadName}' on the workers.");
                await CreateWorkloadAndExecuteAsync(broadcaster, workerConnection, workloadName, cancellationToken, failSignal);

                // Worker Teardown
                logger.LogInformation("Send Teardown command to workers and wait complete message.");
                broadcaster.Teardown();
                await workerConnection.OnTeardown.WaitWithTimeoutAsync(options.Timeout, cancellationToken, failSignal.Task);

                options.OnExecuteResult?.Invoke(workerConnection.ExecuteResult.ToArray(), options, new ExecutedWorkloadInfo(workloadName, workerCount, maxWorkloadPerWorker, executePerWorkload));

                // Worker Shutdown
                broadcaster.Shutdown();

                // MasterTeardown
                if (master != null)
                {
                    logger.LogTrace("Invoke MasterTeardown");
                    using (var linkedToken = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken))
                    {
                        linkedToken.CancelAfter(options.Timeout);
                        await master.TeardownAsync(linkedToken.Token);
                    }
                }

                await Task.Delay(TimeSpan.FromSeconds(1)); // wait Shutdown complete?
            }

            logger.LogInformation("Master shutdown.");
        }

        IHost StartMasterHost(string?[] arguments, CancellationToken cancellationToken)
        {
            var host = options.HostBuilderFactory(arguments)
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.ConfigureKestrel(kestrel =>
                    {
                        kestrel.Listen(IPAddress.Parse(options.MasterListenHost), options.MasterListenPort, listenOptions =>
                        {
                            listenOptions.Protocols = Microsoft.AspNetCore.Server.Kestrel.Core.HttpProtocols.Http2;
                        });
                    });
                    webBuilder.UseStartup(x => new RunnerStartup(options));
                })
                .ConfigureServices(x =>
                {
                    x.AddSingleton<WorkerConnectionGroupContext>();
                    x.AddSingleton(typeof(KeyedValueProvider<>));
                })
                .ConfigureLogging(logging =>
                {
                    if (options.ConfigureInnerHostLogging != null)
                    {
                        options.ConfigureInnerHostLogging(logging);
                    }
                    else
                    {
                        logging.ClearProviders();
                        logging.AddZLoggerConsole(options =>
                        {
                            // todo: configure structured logging
                            options.EnableStructuredLogging = false;
                        });
                    }
                })
                .Build();

            logger.LogInformation("Starting DFrame master.");

            var task = host.RunAsync(cancellationToken);
            if (task.IsFaulted)
            {
                if (task.Exception?.InnerException != null)
                {
                    ExceptionDispatchInfo.Throw(task.Exception.InnerException);
                }
            }

            return host;
        }

        public class RunnerStartup
        {
            DFrameOptions options;

            public RunnerStartup(DFrameOptions options)
            {
                this.options = options;
            }

            public void ConfigureServices(IServiceCollection services)
            {
                services.AddGrpc();
                services.AddMagicOnion(searchTypes: new Type[]
                {
                    typeof(MasterHub),
                    typeof(DistributedQueueService),
                    typeof(DistributedStackService),
                    typeof(DistributedHashSetService),
                    typeof(DistributedListService),
                    typeof(DistributedDictionaryService),
                }, opt =>
                {
                    opt.IsReturnExceptionStackTraceInErrorDetail = true;
                    opt.SerializerOptions = options.SerializerOptions;
                });
            }

            public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
            {
                app.UseRouting();
                app.UseEndpoints(endpoints =>
                {
                    endpoints.MapMagicOnionService();
                    endpoints.MapGet("/", async context =>
                    {
                        await context.Response.WriteAsync("Communication with gRPC endpoints must be made through a gRPC client. To learn how to create a client, visit: https://go.microsoft.com/fwlink/?linkid=2086909");
                    });
                });
            }
        }
    }

    internal sealed class DFrameConcurrentRequestRunner : DFrameRunnerBase
    {
        readonly int workloadPerWorker;
        readonly int executePerWorkload;

        public DFrameConcurrentRequestRunner(ILogger<DFrameApp> logger, IServiceProvider provider, DFrameOptions options, DFrameWorkloadCollection workers, int workloadPerWorker, int executePerWorkload)
            : base(logger, provider, options, workers)
        {
            this.workloadPerWorker = workloadPerWorker;
            this.executePerWorkload = executePerWorkload;
        }

        protected override async Task CreateWorkloadAndExecuteAsync(IWorkerReceiver broadcaster, WorkerConnectionGroupContext workerConnection, string workloadName, CancellationToken cancellationToken, TaskFailSignal failSignal)
        {
            // Worker CreateWorkload
            logger.LogInformation("Send CreateWorkload/Setup command to workers and wait complete message.");
            broadcaster.CreateWorkloadAndSetup(workloadPerWorker, workloadName);
            await workerConnection.OnCreateWorkloadAndSetup.WaitWithTimeoutAsync(options.Timeout, cancellationToken, failSignal.Task);

            // Worker Execute
            logger.LogInformation("Send Execute command to workers and wait complete message.");
            broadcaster.Execute(executePerWorkload);
            await workerConnection.OnExecute.WaitWithTimeoutAsync(options.Timeout, cancellationToken, failSignal.Task);
        }
    }

    internal sealed class DFrameRampupRunner : DFrameRunnerBase
    {
        readonly int maxWorkloadPerWorker;
        readonly int workloadSpawnCount;
        readonly int workloadSpawnSecond;

        public DFrameRampupRunner(ILogger<DFrameApp> logger, IServiceProvider provider, DFrameOptions options, DFrameWorkloadCollection workers, int maxWorkloadPerWorker, int workloadSpawnCount, int workloadSpawnSecond)

            : base(logger, provider, options, workers)
        {
            this.maxWorkloadPerWorker = maxWorkloadPerWorker;
            this.workloadSpawnCount = workloadSpawnCount;
            this.workloadSpawnSecond = workloadSpawnSecond;
        }

        protected override async Task CreateWorkloadAndExecuteAsync(IWorkerReceiver broadcaster, WorkerConnectionGroupContext workerConnection, string workloadName, CancellationToken cancellationToken, TaskFailSignal failSignal)
        {
            var loopCount = maxWorkloadPerWorker / workloadSpawnCount;
            for (int i = 0; i < loopCount; i++)
            {
                // Worker CreateWorkload
                logger.LogInformation($"Send CreateWorkload/Setup command to workers and wait complete message.(RampUpStep={i+1}/{loopCount}; WorkloadSpawnCount={workloadSpawnCount})");
                workerConnection.OnCreateWorkloadAndSetup.Reset();
                broadcaster.CreateWorkloadAndSetup(workloadSpawnCount, workloadName);
                await workerConnection.OnCreateWorkloadAndSetup.WaitWithTimeoutAsync(options.Timeout, cancellationToken, failSignal.Task);

                // Worker Execute
                if (i == 0)
                {
                    logger.LogInformation("Send Execute command to workers.");
                    broadcaster.ExecuteUntilReceiveStop();
                }

                // Wait Spawn.
                await Task.Delay(TimeSpan.FromSeconds(workloadSpawnSecond));
            }

            // Send Stop Command
            logger.LogInformation("Send Stop command to workers.");
            broadcaster.Stop();

            // Wait Execute Complete.
            await workerConnection.OnExecute.WaitWithTimeoutAsync(options.Timeout, cancellationToken, failSignal.Task);
        }
    }
}