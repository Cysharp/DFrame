using System;
using System.Threading.Tasks;

namespace DFrame
{
    public interface IFailSignal
    {
        void TrySetException(Exception ex);
    }

    internal sealed class TaskFailSignal : IFailSignal
    {
        readonly TaskCompletionSource<object> completionSource;

        public Task Task => completionSource.Task;

        public TaskFailSignal()
        {
            completionSource = new TaskCompletionSource<object>();
        }

        public void TrySetException(Exception ex)
        {
            completionSource.TrySetException(ex);
        }
    }
}
