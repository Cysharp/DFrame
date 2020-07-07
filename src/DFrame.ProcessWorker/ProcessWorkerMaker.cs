#pragma warning disable CS1998

using Cysharp.Diagnostics;
using DFrame.Core;
using System;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace DFrame
{
    public class OutOfProcessScalingProvider : IScalingProvider
    {
        CancellationTokenSource cts = new CancellationTokenSource();

        public async Task StartWorkerAsync(DFrameOptions options, int nodeCount, CancellationToken cancellationToken)
        {
            var location = Assembly.GetEntryAssembly().Location;

            var cmd = $"dotnet \"{location}\" --worker-flag";

            for (int i = 0; i < nodeCount; i++)
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
                    Console.WriteLine(item); // TODO:logger?
                }
            }
            catch (OperationCanceledException)
            {
            }
        }

        public ValueTask DisposeAsync()
        {
            cts.Cancel();
            return default;
        }
    }
}