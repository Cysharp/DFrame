using MagicOnion;
using MagicOnion.Server;
using MessagePack;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace DFrame.Core.Collections
{
    public interface IDistributedQueue<T> : IService<IDistributedQueue<T>>
    {
        UnaryResult<int> CountAsync();
        UnaryResult<Nil> ClearAsync();
        UnaryResult<bool> ContainsAsync(T item);
        UnaryResult<Nil> CopyToAsync(T[] array, int arrayIndex);
        UnaryResult<T> DequeueAsync();
        UnaryResult<Nil> EnqueueAsync(T item);
        UnaryResult<T> PeekAsync();
        UnaryResult<T[]> ToArrayAsync();
        UnaryResult<Nil> TrimExcessAsync();
    }

    public class DistributedQueue<T> : ServiceBase<IDistributedQueue<T>>, IDistributedQueue<T>
    {
        static readonly ConcurrentDictionary<string, Queue<T>> dict = new ConcurrentDictionary<string, Queue<T>>();

        Queue<T> GetQueue()
        {
            var key = this.Context.CallContext.RequestHeaders.GetValue("queue-key");
            var q = dict.GetOrAdd(key, _ => new Queue<T>());
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

        public UnaryResult<bool> ContainsAsync(T item)
        {
            var q = GetQueue();
            lock (q)
            {
                return UnaryResult(q.Contains(item));
            }
        }

        public UnaryResult<Nil> CopyToAsync(T[] array, int arrayIndex)
        {
            var q = GetQueue();
            lock (q)
            {
                q.CopyTo(array, arrayIndex);
            }
            return ReturnNil();
        }

        public UnaryResult<T> DequeueAsync()
        {
            var q = GetQueue();
            lock (q)
            {
                return UnaryResult(q.Dequeue());
            }
        }

        public UnaryResult<Nil> EnqueueAsync(T item)
        {
            var q = GetQueue();
            lock (q)
            {
                q.Enqueue(item);
            }
            return ReturnNil();
        }

        public UnaryResult<T> PeekAsync()
        {
            var q = GetQueue();
            lock (q)
            {
                return UnaryResult(q.Peek());
            }
        }

        public UnaryResult<T[]> ToArrayAsync()
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
}