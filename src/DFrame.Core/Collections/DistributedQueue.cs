using MagicOnion;
using MagicOnion.Server;
using MessagePack;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DFrame.Core.Collections
{
    public interface IDistributedQueueService : IService<IDistributedQueueService>
    {
        UnaryResult<int> CountAsync();
        UnaryResult<Nil> ClearAsync();
        UnaryResult<bool> ContainsAsync(object item);
        UnaryResult<(bool, object)> TryDequeueAsync();
        UnaryResult<Nil> EnqueueAsync(object item);
        UnaryResult<(bool, object)> TryPeekAsync();
        UnaryResult<object[]> ToArrayAsync();
        UnaryResult<Nil> TrimExcessAsync();
    }

    public class DistributedQueueService : ServiceBase<IDistributedQueueService>, IDistributedQueueService
    {
        static readonly ConcurrentDictionary<string, Queue<object>> dict = new ConcurrentDictionary<string, Queue<object>>();

        Queue<object> GetQueue()
        {
            var key = this.Context.CallContext.RequestHeaders.GetValue("queue-key");
            var q = dict.GetOrAdd(key, _ => new Queue<object>());
            return q;
        }

        public UnaryResult<int> CountAsync()
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

        public UnaryResult<(bool, object)> TryDequeueAsync()
        {
            var q = GetQueue();
            lock (q)
            {
                if (q.Count == 0)
                {
                    return UnaryResult((false, (object)null!));
                }
                else
                {
                    return UnaryResult((true, q.Dequeue()));
                }
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

        public UnaryResult<(bool, object)> TryPeekAsync()
        {
            var q = GetQueue();
            lock (q)
            {
                if (q.Count == 0)
                {
                    return UnaryResult((false, (object)null!));
                }
                else
                {
                    return UnaryResult((true, q.Peek()));
                }
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

        public UnaryResult<Nil> TrimExcessAsync()
        {
            var q = GetQueue();
            lock (q)
            {
                q.TrimExcess();
            }
            return ReturnNil();
        }
    }

    public interface IDistributedQueue<T>
    {
        Task<int> CountAsync();
        Task<Nil> ClearAsync();
        Task<bool> ContainsAsync(T item);
        Task<(bool, T)> TryDequeueAsync();
        Task<Nil> EnqueueAsync(T item);
        Task<(bool, T)> TryPeekAsync();
        Task<T[]> ToArrayAsync();
        Task<Nil> TrimExcessAsync();
    }

    internal sealed class DistributedQueue<T> : IDistributedQueue<T>
    {
        readonly IDistributedQueueService client;

        internal DistributedQueue(IDistributedQueueService client)
        {
            this.client = client;
        }

        public async Task<Nil> ClearAsync()
        {
            return await client.ClearAsync();
        }

        public async Task<bool> ContainsAsync(T item)
        {
            return await client.ContainsAsync(item!);
        }

        public async Task<int> CountAsync()
        {
            return await client.CountAsync();
        }

        public async Task<(bool, T)> TryDequeueAsync()
        {
            var (ok, value) = await client.TryDequeueAsync();
            if (ok)
            {
                return (true, (T)value);
            }
            else
            {
                return (false, default);
            }
        }

        public async Task<Nil> EnqueueAsync(T item)
        {
            return await client.EnqueueAsync(item!);
        }

        public async Task<(bool, T)> TryPeekAsync()
        {
            var (ok, value) = await client.TryPeekAsync();
            if (ok)
            {
                return (true, (T)value);
            }
            else
            {
                return (false, default);
            }
        }

        public async Task<T[]> ToArrayAsync()
        {
            return (await client.ToArrayAsync()).Cast<T>().ToArray();
        }

        public async Task<Nil> TrimExcessAsync()
        {
            return await client.TrimExcessAsync();
        }
    }
}