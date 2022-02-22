using System.Threading;
using System.Threading.Tasks;

namespace DFrame.Internal
{
    internal static class TaskExtensions
    {
        public static CancellationToken ToCancellationToken(this Task task)
        {
            var cts = new CancellationTokenSource();

            async void RaiseOnCompleted()
            {
                await task;
                cts!.Cancel();
            }

            RaiseOnCompleted();
            return cts.Token;
        }
    }
}