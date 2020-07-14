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
    public interface IDistributedDictionary<TKey, TValue> : IDistributedCollection<KeyValuePair<TKey, TValue>>
    {
        Task ClearAsync();
        Task<bool> ContainsKeyAsync(TKey key);
        Task<bool> ContainsValueAsync(TValue value);
        Task AddAsync(TKey key, TValue value);
        Task AddRangeAsync(IEnumerable<KeyValuePair<TKey, TValue>> collection);
        Task<TValue> GetOrAddAsync(TKey key, TValue value);
        Task SetAsync(TKey key, TValue value);
        Task SetRangeAsync(IEnumerable<KeyValuePair<TKey, TValue>> collection);
        Task<bool> TryAddAsync(TKey key, TValue value);
        Task<ConditionalValue<TValue>> TryGetValueAsync(TKey key);
        Task<ConditionalValue<TValue>> TryRemoveAsync(TKey key);
        Task<bool> TryUpdateAsync(TKey key, TValue value);
    }
}

namespace DFrame.Collections
{
    public interface IDistributedDictionaryService : IService<IDistributedDictionaryService>
    {
        UnaryResult<Nil> ClearAsync();
        UnaryResult<bool> ContainsKeyAsync(object key);
        UnaryResult<bool> ContainsValueAsync(object value);
        UnaryResult<Nil> AddAsync(object key, object value);
        UnaryResult<Nil> AddRangeAsync(IEnumerable<KeyValuePair<object, object>> collection);
        UnaryResult<object> GetOrAddAsync(object key, object value);
        UnaryResult<Nil> SetAsync(object key, object value);
        UnaryResult<Nil> SetRangeAsync(IEnumerable<KeyValuePair<object, object>> collection);
        UnaryResult<bool> TryAddAsync(object key, object value);
        UnaryResult<ConditionalValue<object>> TryGetValueAsync(object key);
        UnaryResult<ConditionalValue<object>> TryRemoveAsync(object key);
        UnaryResult<bool> TryUpdateAsync(object key, object value);

        UnaryResult<int> GetCountAsync();
        UnaryResult<KeyValuePair<object, object>[]> ToArrayAsync();
    }

    public sealed class DistributedDictionaryService : ServiceBase<IDistributedDictionaryService>, IDistributedDictionaryService
    {
        public const string Key = "distributed-dictionary-key";

        readonly KeyedValueProvider<Dictionary<object, object>> valueProvider;

        public DistributedDictionaryService(KeyedValueProvider<Dictionary<object, object>> valueProvider)
        {
            this.valueProvider = valueProvider;
        }

        Dictionary<object, object> GetDictionary()
        {
            var key = this.Context.CallContext.RequestHeaders.GetValue(Key);
            return valueProvider.GetValue(key);
        }

        public UnaryResult<Nil> AddAsync(object key, object value)
        {
            var d = GetDictionary();
            lock (d)
            {
                d.Add(key, value);
            }
            return ReturnNil();
        }

        public UnaryResult<Nil> AddRangeAsync(IEnumerable<KeyValuePair<object, object>> collection)
        {
            var d = GetDictionary();
            lock (d)
            {
                foreach (var item in collection)
                {
                    d.Add(item.Key, item.Value);
                }
            }
            return ReturnNil();
        }

        public UnaryResult<Nil> ClearAsync()
        {
            var d = GetDictionary();
            lock (d)
            {
                d.Clear();
            }
            return ReturnNil();
        }

        public UnaryResult<bool> ContainsKeyAsync(object key)
        {
            var d = GetDictionary();
            lock (d)
            {
                return UnaryResult(d.ContainsKey(key));
            }
        }

        public UnaryResult<bool> ContainsValueAsync(object value)
        {
            var d = GetDictionary();
            lock (d)
            {
                return UnaryResult(d.ContainsValue(value));
            }
        }

        public UnaryResult<object> GetOrAddAsync(object key, object value)
        {
            var d = GetDictionary();
            lock (d)
            {
                if (d.TryGetValue(key, out value))
                {
                    return UnaryResult(value);
                }
                else
                {
                    d[key] = value;
                    return UnaryResult(value);
                }
            }
        }

        public UnaryResult<Nil> SetAsync(object key, object value)
        {
            var d = GetDictionary();
            lock (d)
            {
                d[key] = value;
            }
            return ReturnNil();
        }

        public UnaryResult<Nil> SetRangeAsync(IEnumerable<KeyValuePair<object, object>> collection)
        {
            var d = GetDictionary();
            lock (d)
            {
                foreach (var item in collection)
                {
                    d[item.Key] = item.Value;
                }
            }
            return ReturnNil();
        }

        public UnaryResult<bool> TryAddAsync(object key, object value)
        {
            var d = GetDictionary();
            lock (d)
            {
                return UnaryResult(d.TryAdd(key, value));
            }
        }

        public UnaryResult<ConditionalValue<object>> TryGetValueAsync(object key)
        {
            var d = GetDictionary();
            lock (d)
            {
                return UnaryResult(new ConditionalValue<object>(d.TryGetValue(key, out var v), v));
            }
        }

        public UnaryResult<ConditionalValue<object>> TryRemoveAsync(object key)
        {
            var d = GetDictionary();
            lock (d)
            {
                return UnaryResult(new ConditionalValue<object>(d.Remove(key, out var v), v));
            }
        }

        public UnaryResult<bool> TryUpdateAsync(object key, object value)
        {
            var d = GetDictionary();
            lock (d)
            {
                if (d.ContainsKey(key))
                {
                    d[key] = value;
                    return UnaryResult(true);
                }
                else
                {
                    return UnaryResult(false);
                }
            }
        }

        public UnaryResult<int> GetCountAsync()
        {
            var d = GetDictionary();
            lock (d)
            {
                return UnaryResult(d.Count);
            }
        }

        public UnaryResult<KeyValuePair<object, object>[]> ToArrayAsync()
        {
            var d = GetDictionary();
            lock (d)
            {
                return UnaryResult(d.ToArray());
            }
        }
    }

    internal sealed class DistributedDictionary<TKey, TValue> : IDistributedDictionary<TKey, TValue>
    {
        readonly IDistributedDictionaryService client;

        public string Key { get; }

        internal DistributedDictionary(string key, IDistributedDictionaryService client)
        {
            this.Key = key;
            this.client = client;
        }

        public async Task AddAsync(TKey key, TValue value)
        {
            await client.AddAsync(key!, value!);
        }

        public async Task AddRangeAsync(IEnumerable<KeyValuePair<TKey, TValue>> collection)
        {
            await client.AddRangeAsync(collection.Select(x => new KeyValuePair<object, object>(x.Key!, x.Value!)));
        }

        public async Task ClearAsync()
        {
            await client.ClearAsync();
        }

        public async Task<bool> ContainsKeyAsync(TKey key)
        {
            return await client.ContainsKeyAsync(key!);
        }

        public async Task<bool> ContainsValueAsync(TValue value)
        {
            return await client.ContainsValueAsync(value!);
        }

        public async Task<int> GetCountAsync()
        {
            return await client.GetCountAsync();
        }

        public async Task<TValue> GetOrAddAsync(TKey key, TValue value)
        {
            return (TValue)await client.GetOrAddAsync(key!, value!);
        }

        public async Task SetAsync(TKey key, TValue value)
        {
            await client.SetAsync(key!, value!);
        }

        public async Task SetRangeAsync(IEnumerable<KeyValuePair<TKey, TValue>> collection)
        {
            await client.SetRangeAsync(collection.Select(x => new KeyValuePair<object, object>(x.Key!, x.Value!)));
        }

        public async Task<KeyValuePair<TKey, TValue>[]> ToArrayAsync()
        {
            return (await client.ToArrayAsync()).Select(x => new KeyValuePair<TKey, TValue>((TKey)x.Key!, (TValue)x.Value!)).ToArray();
        }

        public async Task<bool> TryAddAsync(TKey key, TValue value)
        {
            return await client.TryAddAsync(key!, value!);
        }

        public async Task<ConditionalValue<TValue>> TryGetValueAsync(TKey key)
        {
            return (await client.TryGetValueAsync(key!)).As<TValue>();
        }

        public async Task<ConditionalValue<TValue>> TryRemoveAsync(TKey key)
        {
            return (await client.TryRemoveAsync(key!)).As<TValue>();
        }

        public async Task<bool> TryUpdateAsync(TKey key, TValue value)
        {
            return await client.TryUpdateAsync(key!, value!);
        }
    }
}