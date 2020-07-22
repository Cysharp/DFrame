using System;
using System.Threading;
using System.Threading.Tasks;

namespace DFrame
{
    public interface IScalingProvider : IAsyncDisposable
    {
        Task StartWorkerAsync(DFrameOptions options, int processCount, IServiceProvider provider, IFailSignal failSignal, CancellationToken cancellationToken);
    }
}