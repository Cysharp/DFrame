using DFrame.Internal;
using MagicOnion;
using MagicOnion.Server;
using MessagePack;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DFrame
{
    public interface IDistributedQueue<T> : IDistributedCollection<T>
    {
        Task ClearAsync();
        Task<bool> ContainsAsync(T item);
        Task<ConditionalValue<T>> TryDequeueAsync();
        Task EnqueueAsync(T item);
        Task EnqueueRangeAsync(IEnumerable<T> collection);
        Task<ConditionalValue<T>> TryPeekAsync();
    }
}

namespace DFrame.Collections
{
    public interface IDistributedQueueService : IService<IDistributedQueueService>
    {
        UnaryResult<int> GetCountAsync();
        UnaryResult<Nil> ClearAsync();
        UnaryResult<bool> ContainsAsync(object item);
        UnaryResult<ConditionalValue<object>> TryDequeueAsync();
        UnaryResult<Nil> EnqueueAsync(object item);
        UnaryResult<Nil> EnqueueRangeAsync(IEnumerable<object> collection);
        UnaryResult<ConditionalValue<object>> TryPeekAsync();
        UnaryResult<object[]> ToArrayAsync();
    }

    public sealed class DistributedQueueService : ServiceBase<IDistributedQueueService>, IDistributedQueueService
    {
        public const string Key = "distributed-queue-key";

        readonly KeyedValueProvider<Queue<object>> valueProvider;

        public DistributedQueueService(KeyedValueProvider<Queue<object>> valueProvider)
        {
            this.valueProvider = valueProvider;
        }

        Queue<object> GetQueue()
        {
            var key = this.Context.CallContext.RequestHeaders.GetValue(Key);
            return valueProvider.GetValue(key);
        }

        public UnaryResult<int> GetCountAsync()
        {
            var q = GetQueue();
            lock (q)
            {
                return UnaryResult(q.Count);
            }
        }

        public UnaryResult<Nil> ClearAsync()
        {
            var q = GetQueue();
            lock (q)
            {
                q.Clear();
            }
            return ReturnNil();
        }

        public UnaryResult<bool> ContainsAsync(object item)
        {
            var q = GetQueue();
            lock (q)
            {
                return UnaryResult(q.Contains(item));
            }
        }

        public UnaryResult<ConditionalValue<object>> TryDequeueAsync()
        {
            var q = GetQueue();
            lock (q)
            {
                return UnaryResult(new ConditionalValue<object>(q.TryDequeue(out var v), v));
            }
        }

        public UnaryResult<Nil> EnqueueAsync(object item)
        {
            var q = GetQueue();
            lock (q)
            {
                q.Enqueue(item);
            }
            return ReturnNil();
        }

        public UnaryResult<Nil> EnqueueRangeAsync(IEnumerable<object> item)
        {
            var q = GetQueue();
            lock (q)
            {
                foreach (var v in item)
                {
                    q.Enqueue(v);
                }
            }
            return ReturnNil();
        }

        public UnaryResult<ConditionalValue<object>> TryPeekAsync()
        {
            var q = GetQueue();
            lock (q)
            {
                return UnaryResult(new ConditionalValue<object>(q.TryPeek(out var v), v));
            }
        }

        public UnaryResult<object[]> ToArrayAsync()
        {
            var q = GetQueue();
            lock (q)
            {
                return UnaryResult(q.ToArray());
            }
        }
    }

    internal sealed class DistributedQueue<T> : IDistributedQueue<T>
    {
        readonly IDistributedQueueService client;

        public string Key { get; }

        internal DistributedQueue(string key, IDistributedQueueService client)
        {
            this.Key = key;
            this.client = client;
        }

        public async Task ClearAsync()
        {
            await client.ClearAsync();
        }

        public async Task<bool> ContainsAsync(T item)
        {
            return await client.ContainsAsync(item!);
        }

        public async Task<int> GetCountAsync()
        {
            return await client.GetCountAsync();
        }

        public async Task<ConditionalValue<T>> TryDequeueAsync()
        {
            return (await client.TryDequeueAsync()).As<T>();
        }

        public async Task EnqueueAsync(T item)
        {
            await client.EnqueueAsync(item!);
        }

        public async Task<ConditionalValue<T>> TryPeekAsync()
        {
            return (await client.TryPeekAsync()).As<T>();
        }

        public async Task<T[]> ToArrayAsync()
        {
            return (await client.ToArrayAsync()).Cast<T>().ToArray();
        }

        public async Task EnqueueRangeAsync(IEnumerable<T> collection)
        {
            await client.EnqueueRangeAsync(collection.Cast<object>());
        }
    }
}