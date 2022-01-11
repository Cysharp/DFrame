using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace DFrame.Internal
{
    internal static class TaskExtensions
    {
        internal static async Task WithCancellation(this Task task, CancellationToken cancellationToken)
        {
            var tcs = new TaskCompletionSource<object?>();
            var registration = cancellationToken.Register(static state =>
            {
                var t = (TaskCompletionSource<object?>)state!;
                t.TrySetResult(null);
            }, tcs);

            var result = await Task.WhenAny(task, tcs.Task);

            if (result == tcs.Task)
            {
                throw new OperationCanceledException(cancellationToken);
            }
            else
            {
                registration.Dispose();
                await task; // get original task result.
            }
        }

        internal static async Task WithTimeoutAndCancellationAndTaskSignal(this Task task, TimeSpan timeout, CancellationToken cancellationToken, Task taskSignal)
        {
            // CancellationToken
            var cancellationTask = new TaskCompletionSource<object?>();
            var registration = cancellationToken.Register(static state =>
            {
                var t = (TaskCompletionSource<object?>)state!;
                t.TrySetResult(null);
            }, cancellationTask);

            using (registration)
            using (var timeoutCancellation = new CancellationTokenSource())
            {
                try
                {
                    var timeoutTask = Task.Delay(timeout, timeoutCancellation.Token);

                    var result = await Task.WhenAny(task, cancellationTask.Task, timeoutTask, taskSignal);

                    if (result == cancellationTask.Task)
                    {
                        throw new OperationCanceledException(cancellationToken);
                    }
                    else if (result == timeoutTask)
                    {
                        throw new TimeoutException("Timeout elapsed:" + timeout);

                    }
                    else if (result == taskSignal)
                    {
                        await taskSignal; // throw taskSignal's error.
                    }
                    else
                    {
                        await task; // get original task result.
                    }
                }
                finally
                {
                    timeoutCancellation.Cancel();
                }
            }
        }

        internal static async void Forget(this Task task)
        {
            try
            {
                await task;
            }
            catch (Exception ex)
            {
                // TODO: report error?
                Console.WriteLine(ex);
            }
        }
    }
}
