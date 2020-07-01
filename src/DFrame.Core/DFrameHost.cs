using Grpc.Core;
using System;
using MagicOnion;
using MagicOnion.Server;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DFrame.Core
{

    public interface IWorkerMaker
    {
        Task<T> CreatePodAsync<T>(Channel channel)
            where T : IWorkerHub, IStreamingHub<T, INoneReceiver>;
    }

    public class DFrameHost
    {
        readonly int podCount;
        readonly int workerCount;

        public DFrameHost(int podCount,int workerCount, IWorkerMaker workerMaker)
        {
            // IWorkerMaker
        }


        // RunGroup?

        // RunSingleGroup?

        public async Task RunWorkerAsync<T>()
            where T : IWorkerHub, IStreamingHub<T, INoneReceiver>
        {
            // TODO:execute master setup

            IWorkerMaker maker = null;
            Channel channel = null;
            string workerTypeName = "";

            // Parallel?
            var podsTask = new List<Task<T>>();

            for (int i = 0; i < podCount; i++)
            {
                podsTask.Add(maker.CreatePodAsync<T>(channel));
            }

            var pods = await Task.WhenAll(podsTask);


            await Task.WhenAll(pods.Select(x => x.CreateCoWorkerAsync(workerCount, workerTypeName)));

            await Task.WhenAll(pods.Select(x => x.SetupAsync()));
            await Task.WhenAll(pods.Select(x => x.ExecuteAsync()));
            await Task.WhenAll(pods.Select(x => x.TeardownAsync()));

            // kill pods.
            await Task.WhenAll(pods.Select(x => x.ShutdownAsync()));

            // TODO:execute master shutdown

        }


    }
}
