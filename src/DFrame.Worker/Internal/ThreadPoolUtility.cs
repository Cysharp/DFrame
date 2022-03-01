using System;
using System.Threading;

namespace DFrame.Internal
{
    internal static class ThreadPoolUtility
    {
        internal static void SetMinThread(int threadCount)
        {
            ThreadPool.GetMinThreads(out var minWorker, out var minCompletion);
            if (threadCount <= minWorker && threadCount <= minCompletion)
            {
                return;
            }

            ThreadPool.GetMaxThreads(out var maxWorker, out var maxCompletion);
            if (maxWorker < threadCount || maxCompletion < threadCount)
            {
                ThreadPool.SetMaxThreads(Math.Max(maxWorker, threadCount), Math.Max(maxCompletion, threadCount));
            }

            ThreadPool.SetMinThreads(Math.Max(minWorker, threadCount), Math.Max(minWorker, minCompletion));
        }
    }
}
