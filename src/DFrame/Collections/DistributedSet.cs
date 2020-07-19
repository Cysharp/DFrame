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
    public interface IDistributedHashSet<T> : IDistributedCollection<T>
    {
        Task<bool> AddAsync(T item);
        Task AddRangeAsync(IEnumerable<T> collection);
        Task ClearAsync();
        Task<bool> ContainsAsync(T item);
        Task ExceptWithAsync(IEnumerable<T> other);
        Task IntersectWithAsync(IEnumerable<T> other);
        Task<bool> IsProperSubsetOfAsync(IEnumerable<T> other);
        Task<bool> IsProperSupersetOfAsync(IEnumerable<T> other);
        Task<bool> IsSubsetOfAsync(IEnumerable<T> other);
        Task<bool> IsSupersetOfAsync(IEnumerable<T> other);
        Task<bool> OverlapsAsync(IEnumerable<T> other);
        Task<bool> RemoveAsync(T item);
        Task<bool> SetEqualsAsync(IEnumerable<T> other);
        Task SymmetricExceptWithAsync(IEnumerable<T> other);
        Task<ConditionalValue<T>> TryGetValueAsync(T equalValue);
        Task UnionWithAsync(IEnumerable<T> other);
    }
}

namespace DFrame.Collections
{
    public interface IDistributedHashSetService : IService<IDistributedHashSetService>
    {
        UnaryResult<bool> AddAsync(object item);
        UnaryResult<Nil> AddRangeAsync(IEnumerable<object> collection);
        UnaryResult<Nil> ClearAsync();
        UnaryResult<bool> ContainsAsync(object item);
        UnaryResult<Nil> ExceptWithAsync(IEnumerable<object> other);
        UnaryResult<Nil> IntersectWithAsync(IEnumerable<object> other);
        UnaryResult<bool> IsProperSubsetOfAsync(IEnumerable<object> other);
        UnaryResult<bool> IsProperSupersetOfAsync(IEnumerable<object> other);
        UnaryResult<bool> IsSubsetOfAsync(IEnumerable<object> other);
        UnaryResult<bool> IsSupersetOfAsync(IEnumerable<object> other);
        UnaryResult<bool> OverlapsAsync(IEnumerable<object> other);
        UnaryResult<bool> RemoveAsync(object item);
        UnaryResult<bool> SetEqualsAsync(IEnumerable<object> other);
        UnaryResult<Nil> SymmetricExceptWithAsync(IEnumerable<object> other);
        UnaryResult<ConditionalValue<object>> TryGetValueAsync(object equalValue);
        UnaryResult<Nil> UnionWithAsync(IEnumerable<object> other);
        UnaryResult<int> GetCountAsync();
        UnaryResult<object[]> ToArrayAsync();
    }

    public sealed class DistributedHashSetService : ServiceBase<IDistributedHashSetService>, IDistributedHashSetService
    {
        public const string Key = "distributed-hashset-key";
        readonly KeyedValueProvider<HashSet<object>> valueProvider;

        public DistributedHashSetService(KeyedValueProvider<HashSet<object>> valueProvider)
        {
            this.valueProvider = valueProvider;
        }

        HashSet<object> GetHashSet()
        {
            var key = this.Context.CallContext.RequestHeaders.GetValue(Key);
            return valueProvider.GetValue(key);
        }

        public UnaryResult<bool> AddAsync(object item)
        {
            var set = GetHashSet();
            lock (set)
            {
                return UnaryResult(set.Add(item));
            }
        }

        public UnaryResult<Nil> AddRangeAsync(IEnumerable<object> collection)
        {
            var set = GetHashSet();
            lock (set)
            {
                foreach (var item in collection)
                {
                    set.Add(item);
                }
            }
            return ReturnNil();
        }

        public UnaryResult<Nil> ClearAsync()
        {
            var set = GetHashSet();
            lock (set)
            {
                set.Clear();
            }
            return ReturnNil();
        }

        public UnaryResult<bool> ContainsAsync(object item)
        {
            var set = GetHashSet();
            lock (set)
            {
                return UnaryResult(set.Contains(item));
            }
        }

        public UnaryResult<Nil> ExceptWithAsync(IEnumerable<object> other)
        {
            var set = GetHashSet();
            lock (set)
            {
                set.ExceptWith(other);
            }
            return ReturnNil();
        }

        public UnaryResult<int> GetCountAsync()
        {
            var set = GetHashSet();
            lock (set)
            {
                return UnaryResult(set.Count);
            }
        }

        public UnaryResult<Nil> IntersectWithAsync(IEnumerable<object> other)
        {
            var set = GetHashSet();
            lock (set)
            {
                set.IntersectWith(other);
            }
            return ReturnNil();
        }

        public UnaryResult<bool> IsProperSubsetOfAsync(IEnumerable<object> other)
        {
            var set = GetHashSet();
            lock (set)
            {
                return UnaryResult(set.IsProperSubsetOf(other));
            }
        }

        public UnaryResult<bool> IsProperSupersetOfAsync(IEnumerable<object> other)
        {
            var set = GetHashSet();
            lock (set)
            {
                return UnaryResult(set.IsProperSupersetOf(other));
            }
        }

        public UnaryResult<bool> IsSubsetOfAsync(IEnumerable<object> other)
        {
            var set = GetHashSet();
            lock (set)
            {
                return UnaryResult(set.IsSubsetOf(other));
            }
        }

        public UnaryResult<bool> IsSupersetOfAsync(IEnumerable<object> other)
        {
            var set = GetHashSet();
            lock (set)
            {
                return UnaryResult(set.IsSupersetOf(other));
            }
        }

        public UnaryResult<bool> OverlapsAsync(IEnumerable<object> other)
        {
            var set = GetHashSet();
            lock (set)
            {
                return UnaryResult(set.Overlaps(other));
            }
        }

        public UnaryResult<bool> RemoveAsync(object item)
        {
            var set = GetHashSet();
            lock (set)
            {
                return UnaryResult(set.Remove(item));
            }
        }

        public UnaryResult<bool> SetEqualsAsync(IEnumerable<object> other)
        {
            var set = GetHashSet();
            lock (set)
            {
                return UnaryResult(set.SetEquals(other));
            }
        }

        public UnaryResult<Nil> SymmetricExceptWithAsync(IEnumerable<object> other)
        {
            var set = GetHashSet();
            lock (set)
            {
                set.SymmetricExceptWith(other);
            }
            return ReturnNil();
        }

        public UnaryResult<object[]> ToArrayAsync()
        {
            var set = GetHashSet();
            lock (set)
            {
                return UnaryResult(set.ToArray());
            }
        }

        public UnaryResult<ConditionalValue<object>> TryGetValueAsync(object equalValue)
        {
            var set = GetHashSet();
            lock (set)
            {
                return UnaryResult(new ConditionalValue<object>(set.TryGetValue(equalValue, out var v), v));
            }
        }

        public UnaryResult<Nil> UnionWithAsync(IEnumerable<object> other)
        {
            var set = GetHashSet();
            lock (set)
            {
                set.UnionWith(other);
            }
            return ReturnNil();
        }
    }

    internal sealed class DistributedHashSet<T> : IDistributedHashSet<T>
    {
        readonly IDistributedHashSetService client;

        public string Key { get; }

        internal DistributedHashSet(string key, IDistributedHashSetService client)
        {
            this.Key = key;
            this.client = client;
        }

        public async Task<bool> AddAsync(T item)
        {
            return await client.AddAsync(item!);
        }

        public async Task AddRangeAsync(IEnumerable<T> collection)
        {
            await client.AddRangeAsync(collection.Cast<object>());
        }

        public async Task ClearAsync()
        {
            await client.ClearAsync();
        }

        public async Task<bool> ContainsAsync(T item)
        {
            return await client.ContainsAsync(item!);
        }

        public async Task ExceptWithAsync(IEnumerable<T> other)
        {
            await client.ExceptWithAsync(other.Cast<object>());
        }

        public async Task IntersectWithAsync(IEnumerable<T> other)
        {
            await client.IntersectWithAsync(other.Cast<object>());
        }

        public async Task<bool> IsProperSubsetOfAsync(IEnumerable<T> other)
        {
            return await client.IsProperSubsetOfAsync(other.Cast<object>());
        }

        public async Task<bool> IsProperSupersetOfAsync(IEnumerable<T> other)
        {
            return await client.IsProperSupersetOfAsync(other.Cast<object>());
        }

        public async Task<bool> IsSubsetOfAsync(IEnumerable<T> other)
        {
            return await client.IsSubsetOfAsync(other.Cast<object>());
        }

        public async Task<bool> IsSupersetOfAsync(IEnumerable<T> other)
        {
            return await client.IsSupersetOfAsync(other.Cast<object>());
        }

        public async Task<bool> OverlapsAsync(IEnumerable<T> other)
        {
            return await client.OverlapsAsync(other.Cast<object>());
        }

        public async Task<bool> RemoveAsync(T item)
        {
            return await client.RemoveAsync(item!);
        }

        public async Task<bool> SetEqualsAsync(IEnumerable<T> other)
        {
            return await client.SetEqualsAsync(other.Cast<object>());
        }

        public async Task SymmetricExceptWithAsync(IEnumerable<T> other)
        {
            await client.SymmetricExceptWithAsync(other.Cast<object>());
        }

        public async Task<ConditionalValue<T>> TryGetValueAsync(T equalValue)
        {
            return (await client.TryGetValueAsync(equalValue!)).As<T>();
        }

        public async Task UnionWithAsync(IEnumerable<T> other)
        {
            await client.UnionWithAsync(other.Cast<object>());
        }

        public async Task<int> GetCountAsync()
        {
            return await client.GetCountAsync();
        }

        public async Task<T[]> ToArrayAsync()
        {
            return (await client.ToArrayAsync()).Cast<T>().ToArray();
        }
    }
}