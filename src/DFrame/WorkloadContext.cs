using DFrame.Collections;
using Grpc.Core;
using Grpc.Net.Client;
using MagicOnion;
using MagicOnion.Client;
using MessagePack;
using System;

namespace DFrame
{
    public class WorkloadContext
    {
        readonly GrpcChannel masterChannel;
        readonly MessagePackSerializerOptions serializerOptions;

        public string WorkloadId { get; }

        public WorkloadContext(GrpcChannel masterChannel, DFrameOptions options)
        {
            this.masterChannel = masterChannel;
            this.WorkloadId = Guid.NewGuid().ToString();
            this.serializerOptions = options.SerializerOptions;
        }

        public IDistributedQueue<T> CreateDistributedQueue<T>(string key)
        {
            return new DistributedQueue<T>(key, CreateClient<IDistributedQueueService>(DistributedQueueService.Key, key));
        }

        public IDistributedStack<T> CreateDistributedStack<T>(string key)
        {
            return new DistributedStack<T>(key, CreateClient<IDistributedStackService>(DistributedStackService.Key, key));
        }

        public IDistributedList<T> CreateDistributedList<T>(string key)
        {
            return new DistributedList<T>(key, CreateClient<IDistributedListService>(DistributedListService.Key, key));
        }

        public IDistributedHashSet<T> CreateDistributedHashSet<T>(string key)
        {
            return new DistributedHashSet<T>(key, CreateClient<IDistributedHashSetService>(DistributedHashSetService.Key, key));
        }

        public IDistributedDictionary<TKey, TValue> CreateDistributedDictionary<TKey, TValue>(string key)
        {
            return new DistributedDictionary<TKey, TValue>(key, CreateClient<IDistributedDictionaryService>(DistributedDictionaryService.Key, key));
        }

        T CreateClient<T>(string key, string value)
            where T : IService<T>
        {
            return MagicOnionClient.Create<T>(
                    masterChannel.CreateCallInvoker(),
                    serializerOptions)
                .WithHeaders(new Metadata() { { key, value } });
        }
    }
}