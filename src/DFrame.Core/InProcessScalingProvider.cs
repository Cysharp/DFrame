using Grpc.Core;
using MagicOnion.Client;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace DFrame
{
    public class InProcessScalingProvider : IScalingProvider
    {
        List<(Channel, IMasterHub)> channels = new List<(Channel, IMasterHub)>();

        public async Task StartWorkerAsync(DFrameOptions options, int nodeCount, CancellationToken cancellationToken)
        {
            var tasks = new Task[nodeCount];
            for (int i = 0; i < nodeCount; i++)
            {
                tasks[i] = Core(options);
            }

            await Task.WhenAll(tasks);
        }

        async Task Core(DFrameOptions options)
        {
            var channel = new Channel(options.Host, options.Port, ChannelCredentials.Insecure);
            var receiver = new WorkerReceiver(channel);
            var client = StreamingHubClient.Connect<IMasterHub, IWorkerReceiver>(channel, receiver);
            receiver.Client = client;

            lock (channels)
            {
                channels.Add((channel, client));
            }

            await client.ConnectCompleteAsync();
        }

        public async ValueTask DisposeAsync()
        {
            foreach (var item in channels)
            {
                await item.Item2.DisposeAsync();
                await item.Item1.ShutdownAsync();
            }
        }
    }
}