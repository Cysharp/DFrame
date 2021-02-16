using DFrame;
using EchoMagicOnion.Shared;
using Grpc.Core;
using Grpc.Net.Client;
using MagicOnion.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace WebApp
{
    public class SampleWorker : Worker
    {
        IDistributedQueue<int> queue;

        public override async Task SetupAsync(WorkerContext context)
        {
            queue = context.CreateDistributedQueue<int>("sampleworker-testq");
        }

        public override async Task ExecuteAsync(WorkerContext context)
        {
            var randI = (int)new Random().Next(1, 3999);
            //Console.WriteLine($"Enqueue from {Environment.MachineName} {context.WorkerId}: {randI}");

            await queue.EnqueueAsync(randI);
        }

        public override async Task TeardownAsync(WorkerContext context)
        {
            while (true)
            {
                var v = await queue.TryDequeueAsync();
                if (v.HasValue)
                {
                    //Console.WriteLine($"Dequeue all from {Environment.MachineName} {context.WorkerId}: {v.Value}");
                }
                else
                {
                    return;
                }
            }
        }
    }

    public class SampleHttpWorker : Worker
    {
        private static HttpClient httpClient;

        private readonly string _url = "http://localhost:5000";
        private CancellationTokenSource cts;

        static SampleHttpWorker()
        {
            var handler = new HttpClientHandler
            {
                // MaxConnectionsPerServer = 100,
            };
            httpClient = new HttpClient(handler);
            httpClient.DefaultRequestHeaders.Add("ContentType", "application/json");

            // keepalive=false
            //httpClient.DefaultRequestHeaders.Add("Connection", "close");

            // Console.WriteLine($"MaxConnectionsPerServer: {handler.MaxConnectionsPerServer}");
        }

        public override async Task SetupAsync(WorkerContext context)
        {
            cts = new CancellationTokenSource(TimeSpan.FromMinutes(10));
        }

        public override async Task ExecuteAsync(WorkerContext context)
        {
            await httpClient.GetAsync(_url, cts.Token);
            // await r.Content.ReadAsByteArrayAsync();
        }

        public override async Task TeardownAsync(WorkerContext context)
        {
        }
    }

    public class SampleUnaryWorker : Worker
    {
        private GrpcChannel _channel;
        private IEchoService _client;

        private readonly string _host = "localhost";

        public override async Task SetupAsync(WorkerContext context)
        {
            _channel = GrpcChannel.ForAddress(_host + ":12346");
            _client = MagicOnionClient.Create<IEchoService>(_channel);
        }
        public override async Task ExecuteAsync(WorkerContext context)
        {
            await _client.Echo(context.WorkerId);
        }

        public override async Task TeardownAsync(WorkerContext context)
        {
            await _channel.ShutdownAsync().ConfigureAwait(false);
        }
    }

    public class SampleStreamWorker : Worker
    {
        private GrpcChannel _channel;
        private IEchoHub _client;

        private readonly string _host = "localhost";

        public override async Task SetupAsync(WorkerContext context)
        {
            _channel = GrpcChannel.ForAddress(_host + ":12346");
            var receiver = new EchoReceiver(_channel);
            _client = await StreamingHubClient.ConnectAsync<IEchoHub, IEchoHubReceiver>(_channel, receiver);
            receiver.Client = _client;
        }
        public override async Task ExecuteAsync(WorkerContext context)
        {
            await _client.EchoAsync(context.WorkerId);
            //await _client.EchoBroadcastAsync(context.WorkerId);
        }

        public override async Task TeardownAsync(WorkerContext context)
        {
            await _client.DisposeAsync();
            await _channel.ShutdownAsync().ConfigureAwait(false);
        }
    }
}
