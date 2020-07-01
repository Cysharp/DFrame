using DFrame.Core.Collections;
using Grpc.Core;
using MagicOnion;
using MagicOnion.Client;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace DFrame.Core
{
    public class InProcessWorkerMaker : IWorkerMaker
    {
        public Task<T> CreatePodAsync<T>(Channel channel) 
            where T : IWorkerHub, IStreamingHub<T, INoneReceiver>
        {
            // throw new NotImplementedException();


            var hub = StreamingHubClient.Connect<T, INoneReceiver>(channel, new Receiver());

            throw new NotImplementedException();
        }

        public class Receiver : INoneReceiver
        {
        }
    }
}
