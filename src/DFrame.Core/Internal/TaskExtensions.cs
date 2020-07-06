using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace DFrame.Core.Internal
{
    internal static class TaskExtensions
    {
        internal static async Task WithCancellation(this Task task, CancellationToken cancellationToken)
        {
            var tcs = new TaskCompletionSource<object?>();
            var registration = cancellationToken.Register(state =>
            {
                var t = (TaskCompletionSource<object?>)state;
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
                result.GetAwaiter().GetResult();
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
