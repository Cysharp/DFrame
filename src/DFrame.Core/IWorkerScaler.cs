using Grpc.Core;
using Microsoft.Extensions.Hosting;
using System;
using System.Threading.Tasks;

namespace DFrame.Core
{
    public interface IWorkerScaler : IDisposable
    {
        Task<Channel> StartWorkerHostAsync(DFrameOptions options, string?[] args, int port);
    }
}