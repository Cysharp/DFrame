using Grpc.Core;
using MagicOnion.Client;
using Microsoft.Extensions.DependencyInjection;
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
        IFailSignal failSignal = default!;

        public Task StartWorkerAsync(DFrameOptions options, int processCount, IServiceProvider provider, IFailSignal failSignal, CancellationToken cancellationToken)
        {
            this.failSignal = failSignal;
            cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

            var tasks = new Task[processCount];
            for (int i = 0; i < processCount; i++)
            {
                tasks[i] = Core(provider, options, cancellationTokenSource.Token);
            }

            this.tasks = Task.WhenAll(tasks);
            return Task.CompletedTask;
        }

        async Task Core(IServiceProvider provider, DFrameOptions options, CancellationToken cancellationToken)
        {
            // create shim of ConsoleApp
            try
            {
                var logger = provider.GetRequiredService(typeof(ILogger<DFrameWorkerApp>));
                var logger2 = provider.GetRequiredService(typeof(ILogger<ConsoleAppFramework.ConsoleApp>));
                var app = new DFrameWorkerApp((ILogger<DFrameWorkerApp>)logger, provider, options);
                app.Context = new ConsoleAppFramework.ConsoleAppContext(new string[0], DateTime.UtcNow, cancellationToken, (ILogger<ConsoleAppFramework.ConsoleApp>)logger2, null!, provider);

                await app.Main();
            }
            catch (Exception ex)
            {
                failSignal.TrySetException(ex);
            }
        }

        public async ValueTask DisposeAsync()
        {
            cancellationTokenSource.Cancel();
            await tasks;
        }
    }
}