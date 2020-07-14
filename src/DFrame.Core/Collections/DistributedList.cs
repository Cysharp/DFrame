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
    public interface IDistributedList<T> : IDistributedCollection<T>
    {
        Task ClearAsync();
        Task<bool> ContainsAsync(T item);
        Task<T> GetValueAsync(int index);
        Task SetValueAsync(int index, T item);
        Task AddAsync(T item);
        Task AddRangeAsync(IEnumerable<T> collection);
        Task<List<T>> GetRangeAsync(int index, int count);
        Task<int> IndexOfAsync(T item);
        Task<int> IndexOfAsync(T item, int index);
        Task<int> IndexOfAsync(T item, int index, int count);
        Task InsertAsync(int index, T item);
        Task InsertRangeAsync(int index, IEnumerable<T> collection);
        Task<int> LastIndexOfAsync(T item);
        Task<int> LastIndexOfAsync(T item, int index);
        Task<int> LastIndexOfAsync(T item, int index, int count);
        Task<bool> RemoveAsync(T item);
        Task RemoveAtAsync(int index);
        Task RemoveRangeAsync(int index, int count);
        Task ReverseAsync();
        Task ReverseAsync(int index, int count);
        Task SortAsync();
    }
}

namespace DFrame.Collections
{
    public interface IDistributedListService : IService<IDistributedListService>
    {
        UnaryResult<int> GetCountAsync();
        UnaryResult<object[]> ToArrayAsync();

        UnaryResult<Nil> ClearAsync();
        UnaryResult<bool> ContainsAsync(object item);
        UnaryResult<object> GetValueAsync(int index);
        UnaryResult<Nil> SetValueAsync(int index, object item);
        UnaryResult<Nil> AddAsync(object item);
        UnaryResult<Nil> AddRangeAsync(IEnumerable<object> collection);
        UnaryResult<List<object>> GetRangeAsync(int index, int count);
        UnaryResult<int> IndexOfAsync(object item);
        UnaryResult<int> IndexOf2Async(object item, int index);
        UnaryResult<int> IndexOf3Async(object item, int index, int count);
        UnaryResult<Nil> InsertAsync(int index, object item);
        UnaryResult<Nil> InsertRangeAsync(int index, IEnumerable<object> collection);
        UnaryResult<int> LastIndexOfAsync(object item);
        UnaryResult<int> LastIndexOf2Async(object item, int index);
        UnaryResult<int> LastIndexOf3Async(object item, int index, int count);
        UnaryResult<bool> RemoveAsync(object item);
        UnaryResult<Nil> RemoveAtAsync(int index);
        UnaryResult<Nil> RemoveRangeAsync(int index, int count);
        UnaryResult<Nil> ReverseAsync();
        UnaryResult<Nil> Reverse2Async(int index, int count);
        UnaryResult<Nil> SortAsync();
    }

    public sealed class DistributedListService : ServiceBase<IDistributedListService>, IDistributedListService
    {
        public const string Key = "distributed-list-key";

        readonly KeyedValueProvider<List<object>> valueProvider;

        public DistributedListService(KeyedValueProvider<List<object>> valueProvider)
        {
            this.valueProvider = valueProvider;
        }

        List<object> GetList()
        {
            var key = this.Context.CallContext.RequestHeaders.GetValue(Key);
            return valueProvider.GetValue(key);
        }

        public UnaryResult<Nil> AddAsync(object item)
        {
            var list = GetList();
            lock (list)
            {
                list.Add(item);
            }
            return ReturnNil();
        }

        public UnaryResult<Nil> AddRangeAsync(IEnumerable<object> collection)
        {
            var list = GetList();
            lock (list)
            {
                list.AddRange(collection);
            }
            return ReturnNil();
        }

        public UnaryResult<Nil> ClearAsync()
        {
            var list = GetList();
            lock (list)
            {
                list.Clear();
            }
            return ReturnNil();
        }

        public UnaryResult<bool> ContainsAsync(object item)
        {
            var list = GetList();
            lock (list)
            {
                return UnaryResult(list.Contains(item));
            }
        }

        public UnaryResult<int> GetCountAsync()
        {
            var list = GetList();
            lock (list)
            {
                return UnaryResult(list.Count);
            }
        }

        public UnaryResult<List<object>> GetRangeAsync(int index, int count)
        {
            var list = GetList();
            lock (list)
            {
                return UnaryResult(list.GetRange(index, count));
            }
        }

        public UnaryResult<object> GetValueAsync(int index)
        {
            var list = GetList();
            lock (list)
            {
                return UnaryResult(list[index]);
            }
        }

        public UnaryResult<int> IndexOf2Async(object item, int index)
        {
            var list = GetList();
            lock (list)
            {
                return UnaryResult(list.IndexOf(item, index));
            }
        }

        public UnaryResult<int> IndexOf3Async(object item, int index, int count)
        {
            var list = GetList();
            lock (list)
            {
                return UnaryResult(list.IndexOf(item, index, count));
            }
        }

        public UnaryResult<int> IndexOfAsync(object item)
        {
            var list = GetList();
            lock (list)
            {
                return UnaryResult(list.IndexOf(item));
            }
        }

        public UnaryResult<Nil> InsertAsync(int index, object item)
        {
            var list = GetList();
            lock (list)
            {
                list.Insert(index, item);
            }
            return ReturnNil();
        }

        public UnaryResult<Nil> InsertRangeAsync(int index, IEnumerable<object> collection)
        {
            var list = GetList();
            lock (list)
            {
                list.InsertRange(index, collection);
            }
            return ReturnNil();
        }

        public UnaryResult<int> LastIndexOf2Async(object item, int index)
        {
            var list = GetList();
            lock (list)
            {
                return UnaryResult(list.LastIndexOf(item, index));
            }
        }

        public UnaryResult<int> LastIndexOf3Async(object item, int index, int count)
        {
            var list = GetList();
            lock (list)
            {
                return UnaryResult(list.LastIndexOf(item, index, count));
            }
        }

        public UnaryResult<int> LastIndexOfAsync(object item)
        {
            var list = GetList();
            lock (list)
            {
                return UnaryResult(list.LastIndexOf(item));
            }
        }

        public UnaryResult<bool> RemoveAsync(object item)
        {
            var list = GetList();
            lock (list)
            {
                return UnaryResult(list.Remove(item));
            }
        }

        public UnaryResult<Nil> RemoveAtAsync(int index)
        {
            var list = GetList();
            lock (list)
            {
                list.RemoveAt(index);
            }
            return ReturnNil();
        }

        public UnaryResult<Nil> RemoveRangeAsync(int index, int count)
        {
            var list = GetList();
            lock (list)
            {
                list.RemoveRange(index, count);
            }
            return ReturnNil();
        }

        public UnaryResult<Nil> Reverse2Async(int index, int count)
        {
            var list = GetList();
            lock (list)
            {
                list.Reverse(index, count);
            }
            return ReturnNil();
        }

        public UnaryResult<Nil> ReverseAsync()
        {
            var list = GetList();
            lock (list)
            {
                list.Reverse();
            }
            return ReturnNil();
        }

        public UnaryResult<Nil> SetValueAsync(int index, object value)
        {
            var list = GetList();
            lock (list)
            {
                list[index] = value;
            }
            return ReturnNil();
        }

        public UnaryResult<Nil> SortAsync()
        {
            var list = GetList();
            lock (list)
            {
                list.Sort();
            }
            return ReturnNil();
        }

        public UnaryResult<object[]> ToArrayAsync()
        {
            var list = GetList();
            lock (list)
            {
                return UnaryResult(list.ToArray());
            }
        }
    }

    internal sealed class DistributedList<T> : IDistributedList<T>
    {
        readonly IDistributedListService client;

        public string Key { get; }

        internal DistributedList(string key, IDistributedListService client)
        {
            this.Key = key;
            this.client = client;
        }

        public async Task AddAsync(T item)
        {
            await client.AddAsync(item!);
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

        public async Task<int> GetCountAsync()
        {
            return await client.GetCountAsync();
        }

        public async Task<List<T>> GetRangeAsync(int index, int count)
        {
            return (await client.GetRangeAsync(index, count)).Cast<T>().ToList();
        }

        public async Task<T> GetValueAsync(int index)
        {
            return (T)await client.GetValueAsync(index);
        }

        public async Task<int> IndexOfAsync(T item)
        {
            return await client.IndexOfAsync(item!);
        }

        public async Task<int> IndexOfAsync(T item, int index)
        {
            return await client.IndexOf2Async(item!, index);
        }

        public async Task<int> IndexOfAsync(T item, int index, int count)
        {
            return await client.IndexOf3Async(item!, index, count);
        }

        public async Task InsertAsync(int index, T item)
        {
            await client.InsertAsync(index, item!);
        }

        public async Task InsertRangeAsync(int index, IEnumerable<T> collection)
        {
            await client.InsertRangeAsync(index, collection.Cast<object>());
        }

        public async Task<int> LastIndexOfAsync(T item)
        {
            return await client.LastIndexOfAsync(item!);
        }

        public async Task<int> LastIndexOfAsync(T item, int index)
        {
            return await client.LastIndexOf2Async(item!, index);
        }

        public async Task<int> LastIndexOfAsync(T item, int index, int count)
        {
            return await client.LastIndexOf3Async(item!, index, count);
        }

        public async Task<bool> RemoveAsync(T item)
        {
            return await client.RemoveAsync(item!);
        }

        public async Task RemoveAtAsync(int index)
        {
            await client.RemoveAtAsync(index);
        }

        public async Task RemoveRangeAsync(int index, int count)
        {
            await client.RemoveRangeAsync(index, count);
        }

        public async Task ReverseAsync()
        {
            await client.ReverseAsync();
        }

        public async Task ReverseAsync(int index, int count)
        {
            await client.Reverse2Async(index, count);
        }

        public async Task SetValueAsync(int index, T item)
        {
            await client.SetValueAsync(index, item!);
        }

        public async Task SortAsync()
        {
            await client.SortAsync();
        }

        public async Task<T[]> ToArrayAsync()
        {
            return (await client.ToArrayAsync()).Cast<T>().ToArray();
        }
    }
}