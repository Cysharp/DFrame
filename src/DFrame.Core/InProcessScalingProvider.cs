using Grpc.Core;
using MagicOnion.Client;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace DFrame
{
    public class InProcessScalingProvider : IScalingProvider
    {
        CancellationTokenSource cancellationTokenSource = default!;
        Task tasks = default!;

        public Task StartWorkerAsync(DFrameOptions options, int nodeCount, IServiceProvider provider, CancellationToken cancellationToken)
        {
            cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

            var tasks = new Task[nodeCount];
            for (int i = 0; i < nodeCount; i++)
            {
                tasks[i] = Core(provider, options, cancellationTokenSource.Token);
            }

            this.tasks = Task.WhenAll(tasks);
            return Task.CompletedTask;
        }

        async Task Core(IServiceProvider provider, DFrameOptions options, CancellationToken cancellationToken)
        {
            // create shim of ConsoleApp
            var logger = provider.GetService(typeof(ILogger<DFrameWorkerApp>));
            var logger2 = provider.GetService(typeof(ILogger<ConsoleAppFramework.ConsoleAppEngine>));
            var app = new DFrameWorkerApp((ILogger<DFrameWorkerApp>)logger, provider, options);
            app.Context = new ConsoleAppFramework.ConsoleAppContext(new string[0], DateTime.UtcNow, cancellationToken, (ILogger<ConsoleAppFramework.ConsoleAppEngine>)logger2);

            await app.Main();
        }

        public async ValueTask DisposeAsync()
        {
            cancellationTokenSource.Cancel();
            await tasks;
        }
    }
}