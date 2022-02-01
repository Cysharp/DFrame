using System;
using System.Threading;

namespace DFrame.Internal
{
    internal static class ThreadPoolUtility
    {
        internal static void SetMinThread(int threadCount)
        {
            ThreadPool.GetMinThreads(out var worker, out var completion);
            if (threadCount == worker && threadCount == completion)
            {
                return;
            }

            ThreadPool.SetMinThreads(Math.Max(worker, threadCount), Math.Max(worker, completion));
        }
    }
}
