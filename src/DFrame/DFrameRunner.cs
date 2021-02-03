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
        protected DFrameWorkerCollection workers;
        protected IHost? masterHost;

        public DFrameRunnerBase(ILogger<DFrameApp> logger, IServiceProvider provider, DFrameOptions options, DFrameWorkerCollection workers)
        {
            this.provider = provider;
            this.logger = logger;
            this.workers = workers;
            this.options = options;
        }

        protected abstract Task CreateCoWorkerAndExecuteAsync(IWorkerReceiver broadcaster, WorkerConnectionGroupContext workerConnection, string workerName, CancellationToken cancellationToken, TaskFailSignal failSignal);

        public async Task RunAsync(
            string workerName,
            int processCount,
            int maxWorkerPerProcess, // TODO: remove it
            int executePerWorker, // TODO: remove it
            ConsoleAppFramework.ConsoleAppContext consoleAppContext) // TODO:remove this context.
        {
            var cancellationToken = consoleAppContext.CancellationToken;
            var args = consoleAppContext.Arguments;

            ThreadPoolUtility.SetMinThread(maxWorkerPerProcess);
            // validate worker is exists.
            if (!workers.TryGetWorker(workerName, out var workerInfo))
            {
                throw new InvalidOperationException($"Worker:{workerName} does not found in assembly.");
            }

            var failSignal = new TaskFailSignal();

            using (masterHost = StartMasterHost(args, cancellationToken))
            await using (options.ScalingProvider)
            {
                var workerConnection = masterHost.Services.GetRequiredService<WorkerConnectionGroupContext>();
                workerConnection.Initialize(processCount, options.WorkerDisconnectedBehaviour == WorkerDisconnectedBehaviour.Stop);

                logger.LogInformation("Starting worker nodes.");
                await options.ScalingProvider.StartWorkerAsync(options, processCount, provider, failSignal, cancellationToken).WithTimeoutAndCancellationAndTaskSignal(options.Timeout, cancellationToken, failSignal.Task);

                // wait worker is connected
                await workerConnection.WaitAllConnectedWithTimeoutAsync(options.Timeout, cancellationToken, failSignal.Task);

                var broadcaster = workerConnection.Broadcaster;

                Master? master = default;
                if (workerInfo.MasterType != null)
                {
                    master = provider.GetRequiredService(workerInfo.MasterType) as Master;
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

                await CreateCoWorkerAndExecuteAsync(broadcaster, workerConnection, workerName, cancellationToken, failSignal);

                // Worker Teardown
                logger.LogTrace("Send SetTeardownup command to workers and wait complete message.");
                broadcaster.Teardown();
                await workerConnection.OnTeardown.WaitWithTimeoutAsync(options.Timeout, cancellationToken, failSignal.Task);

                options.OnExecuteResult?.Invoke(workerConnection.ExecuteResult.ToArray(), options, new ExecuteScenario(workerName, processCount, maxWorkerPerProcess, executePerWorker));

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

            logger.LogInformation("Starting DFrame master node.");

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
                    typeof(IDistributedDictionaryService),
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
        readonly int workerPerProcess;
        readonly int executePerWorker;

        public DFrameConcurrentRequestRunner(ILogger<DFrameApp> logger, IServiceProvider provider, DFrameOptions options, DFrameWorkerCollection workers, int workerPerProcess, int executePerWorker)
            : base(logger, provider, options, workers)
        {
            this.workerPerProcess = workerPerProcess;
            this.executePerWorker = executePerWorker;
        }

        protected override async Task CreateCoWorkerAndExecuteAsync(IWorkerReceiver broadcaster, WorkerConnectionGroupContext workerConnection, string workerName, CancellationToken cancellationToken, TaskFailSignal failSignal)
        {
            // Worker CreateCoWorker
            logger.LogTrace("Send CreateWorker/Setup command to workers and wait complete message.");
            broadcaster.CreateCoWorkerAndSetup(workerPerProcess, workerName);
            await workerConnection.OnCreateCoWorkerAndSetup.WaitWithTimeoutAsync(options.Timeout, cancellationToken, failSignal.Task);

            // Worker Execute
            logger.LogTrace("Send Execute command to workers and wait complete message.");
            broadcaster.Execute(executePerWorker);
            await workerConnection.OnExecute.WaitWithTimeoutAsync(options.Timeout, cancellationToken, failSignal.Task);
        }
    }

    internal sealed class DFrameRamupRunner : DFrameRunnerBase
    {
        readonly int maxWorkerPerProcess;
        readonly int workerSpawnCount;
        readonly int workerSpawnSecond;

        public DFrameRamupRunner(ILogger<DFrameApp> logger, IServiceProvider provider, DFrameOptions options, DFrameWorkerCollection workers, int maxWorkerPerProcess, int workerSpawnCount, int workerSpawnSecond)

            : base(logger, provider, options, workers)
        {
            this.maxWorkerPerProcess = maxWorkerPerProcess;
            this.workerSpawnCount = workerSpawnCount;
            this.workerSpawnSecond = workerSpawnSecond;
        }

        protected override async Task CreateCoWorkerAndExecuteAsync(IWorkerReceiver broadcaster, WorkerConnectionGroupContext workerConnection, string workerName, CancellationToken cancellationToken, TaskFailSignal failSignal)
        {
            var loopCount = maxWorkerPerProcess / workerSpawnCount;
            for (int i = 0; i < loopCount; i++)
            {
                // Worker CreateCoWorker
                logger.LogTrace("Send CreateWorker/Setup command to workers and wait complete message.");
                workerConnection.OnCreateCoWorkerAndSetup.Reset();
                broadcaster.CreateCoWorkerAndSetup(workerSpawnCount, workerName);
                await workerConnection.OnCreateCoWorkerAndSetup.WaitWithTimeoutAsync(options.Timeout, cancellationToken, failSignal.Task);

                // Worker Execute
                if (i == 0)
                {
                    logger.LogTrace("Send Execute command to workers.");
                    broadcaster.ExecuteUntilReceiveStop();
                }

                // Wait Spawn.
                await Task.Delay(TimeSpan.FromSeconds(workerSpawnSecond));
            }

            // Send Stop Command
            logger.LogTrace("Send Stop command to workers.");
            broadcaster.Stop();

            // Wait Execute Complete.
            await workerConnection.OnExecute.WaitWithTimeoutAsync(options.Timeout, cancellationToken, failSignal.Task);
        }
    }
}