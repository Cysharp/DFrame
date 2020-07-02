using ConsoleAppFramework;
using DFrame.Core.Collections;
using DFrame.Core.Internal;
using Grpc.Core;
using MagicOnion.Client;
using MagicOnion.Hosting;
using MagicOnion.Server;
using MessagePack;
using MessagePack.Resolvers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.ExceptionServices;
using System.Threading.Tasks;

namespace DFrame.Core
{
    public static class DFrameAppHostBuilderExtensions
    {
        public static async Task RunDFrameAsync(this IHostBuilder hostBuilder, string[] args, DFrameOptions options)
        {
            await hostBuilder
                .ConfigureServices(x => x.AddSingleton(options))
                .RunConsoleAppFrameworkAsync<DFrameApp>(args);
        }
    }

    internal class DFrameApp : ConsoleAppBase
    {
        DFrameOptions options;
        IHost? masterHost;

        public DFrameApp(DFrameOptions options)
        {
            this.options = options;
        }

        public async Task Main(int workerPerHost, int executePerWorker, string scenarioName)
        {
            using (masterHost = StartMasterHost())
            using (options.WorkerScaler)
            {
                var (count, start, end) = options.WorkerPortRange;
                var channelTasks = new List<Task<Channel>>(count);

                for (int port = start; port <= end; port++)
                {
                    channelTasks.Add(options.WorkerScaler.StartWorkerHostAsync(options, Context.Arguments, port));
                }

                var channels = await Task.WhenAll(channelTasks);


                var hubs = new IWorkerHub[count];
                for (int i = 0; i < hubs.Length; i++)
                {
                    hubs[i] = StreamingHubClient.Connect<IWorkerHub, INoneReceiver>(channels[i], NoneReceiver.Instance);
                }

                await Task.WhenAll(hubs.Select(x => x.CreateCoWorkerAsync(workerPerHost, scenarioName, "localhost:" + options.MasterPort)));
                await Task.WhenAll(hubs.Select(x => x.SetupAsync()));
                await Task.WhenAll(hubs.Select(x => x.ExecuteAsync(executePerWorker)));
                await Task.WhenAll(hubs.Select(x => x.TeardownAsync()));
            }
        }

        IHost StartMasterHost()
        {
            var host = options.HostBuilderFactory(Context.Arguments)
                .UseMagicOnion(targetTypes: new Type[] { typeof(DistributedQueueService) }, options: new MagicOnionOptions
                {
                    IsReturnExceptionStackTraceInErrorDetail = true,
                    SerializerOptions = MessagePackSerializer.Typeless.DefaultOptions // use Typeless.
                }, ports: new ServerPort("localhost", options.MasterPort, ServerCredentials.Insecure)) // TODO:Server host
                .Build();

            var task = host.RunAsync(Context.CancellationToken);
            if (task.IsFaulted)
            {
                ExceptionDispatchInfo.Throw(task.Exception.InnerException);
            }

            return host;
        }
    }
}