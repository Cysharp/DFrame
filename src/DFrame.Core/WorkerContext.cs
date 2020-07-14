using DFrame.Collections;
using Grpc.Core;
using MagicOnion;
using MagicOnion.Client;
using MessagePack;
using System;
using System.Threading.Tasks;

namespace DFrame
{
    public class WorkerContext
    {
        readonly Channel masterChannel;

        public string WorkerId { get; }

        public WorkerContext(Channel masterChannel)
        {
            this.masterChannel = masterChannel;
            this.WorkerId = Guid.NewGuid().ToString();
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
                new DefaultCallInvoker(masterChannel),
                MessagePackSerializer.Typeless.DefaultOptions,
                new IClientFilter[] { new AppendHeaderFilter(key, value) });
        }
    }

    internal class AppendHeaderFilter : IClientFilter
    {
        readonly string key;
        readonly string value;

        public AppendHeaderFilter(string key, string value)
        {
            this.key = key;
            this.value = value;
        }

        public ValueTask<ResponseContext> SendAsync(RequestContext context, Func<RequestContext, ValueTask<ResponseContext>> next)
        {
            context.CallOptions.Headers.Add(key, value);
            return next(context);
        }
    }
}