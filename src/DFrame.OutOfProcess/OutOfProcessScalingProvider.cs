#pragma warning disable CS1998

using Cysharp.Diagnostics;
using System;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace DFrame
{
    public class OutOfProcessScalingProvider : IScalingProvider
    {
        CancellationTokenSource cts = new CancellationTokenSource();
        IFailSignal failSignal = default!;

        public async Task StartWorkerAsync(DFrameOptions options, int processCount, IServiceProvider provider, IFailSignal failSignal, CancellationToken cancellationToken)
        {
            this.failSignal = failSignal;

            var location = Assembly.GetEntryAssembly()!.Location;

            var cmd = $"dotnet \"{location}\" --worker-flag";

            for (int i = 0; i < processCount; i++)
            {
                var startProcessTask = ProcessX.StartAsync(cmd);
                WriteAll(startProcessTask);
            }
        }

        async void WriteAll(ProcessAsyncEnumerable e)
        {
            try
            {
                await foreach (var item in e.WithCancellation(cts.Token))
                {
                    Console.WriteLine(item);
                }
            }
            catch (Exception ex)
            {
                if (ex is OperationCanceledException) return;
                failSignal.TrySetException(ex);
            }
        }

        public ValueTask DisposeAsync()
        {
            cts.Cancel();
            return default;
        }
    }
}