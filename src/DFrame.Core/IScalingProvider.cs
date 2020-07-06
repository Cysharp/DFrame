using Grpc.Core;
using Microsoft.Extensions.Hosting;
using System;
using System.Threading.Tasks;

namespace DFrame.Core
{
    public interface IScalingProvider : IAsyncDisposable
    {
        Task StartWorkerChannelAsync(DFrameOptions options);
    }
}