#pragma warning disable CS1998

using Cysharp.Diagnostics;
using DFrame.Core;
using Grpc.Core;
using MagicOnion;
using MagicOnion.Client;
using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace DFrame.ProcessWorker
{
    //public class ProcessWorkerMaker : IWorkerMaker
    //{
    //    readonly Process parentProcess;

    //    public ProcessWorkerMaker(Process parentProcess)
    //    {
    //        this.parentProcess = parentProcess;
    //    }

    //    public Task<T> CreatePodAsync<T>(Channel channel)
    //        where T : IWorkerHub, IStreamingHub<T, INoneReceiver>
    //    {
    //        throw new NotImplementedException();
    //    }

    //    //public async Task<T> CreatePodAsync<T>(Channel channel)
    //    //    where T: IWorker
    //    //{

    //    //    // throw new NotImplementedException();

    //    //    //Process.Start(new ProcessStartInfo
    //    //    //{


    //    //    var foo = ProcessX.StartAsync(fileName: parentProcess.StartInfo.FileName, arguments: "argument");


    //    //    // 0
    //    //    // channel.ConnectAsync();




    //    //    return MagicOnionClient.Create<T>(channel);
    //    //}
    //}
}
