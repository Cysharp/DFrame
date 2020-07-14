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
    public interface IDistributedStack<T> : IDistributedCollection<T>
    {
        Task ClearAsync();
        Task<bool> ContainsAsync(T item);
        Task<ConditionalValue<T>> TryPopAsync();
        Task PushAsync(T item);
        Task PushRangeAsync(IEnumerable<T> collection);
        Task<ConditionalValue<T>> TryPeekAsync();
    }
}

namespace DFrame.Collections
{
    public interface IDistributedStackService : IService<IDistributedStackService>
    {
        UnaryResult<int> GetCountAsync();
        UnaryResult<Nil> ClearAsync();
        UnaryResult<bool> ContainsAsync(object item);
        UnaryResult<ConditionalValue<object>> TryPopAsync();
        UnaryResult<Nil> PushAsync(object item);
        UnaryResult<Nil> PushRangeAsync(IEnumerable<object> collection);
        UnaryResult<ConditionalValue<object>> TryPeekAsync();
        UnaryResult<object[]> ToArrayAsync();
    }

    public sealed class DistributedStackService : ServiceBase<IDistributedStackService>, IDistributedStackService
    {
        public const string Key = "distributed-stack-key";
        readonly KeyedValueProvider<Stack<object>> valueProvider;

        public DistributedStackService(KeyedValueProvider<Stack<object>> valueProvider)
        {
            this.valueProvider = valueProvider;
        }

        Stack<object> GetStack()
        {
            var key = this.Context.CallContext.RequestHeaders.GetValue(Key);
            return valueProvider.GetValue(key);
        }

        public UnaryResult<int> GetCountAsync()
        {
            var stack = GetStack();
            lock (stack)
            {
                return UnaryResult(stack.Count);
            }
        }

        public UnaryResult<Nil> ClearAsync()
        {
            var stack = GetStack();
            lock (stack)
            {
                stack.Clear();
            }
            return ReturnNil();
        }

        public UnaryResult<bool> ContainsAsync(object item)
        {
            var stack = GetStack();
            lock (stack)
            {
                return UnaryResult(stack.Contains(item));
            }
        }

        public UnaryResult<ConditionalValue<object>> TryPopAsync()
        {
            var stack = GetStack();
            lock (stack)
            {
                return UnaryResult(new ConditionalValue<object>(stack.TryPop(out var result), result));
            }
        }

        public UnaryResult<Nil> PushAsync(object item)
        {
            var stack = GetStack();
            lock (stack)
            {
                stack.Push(item);
            }
            return ReturnNil();
        }

        public UnaryResult<Nil> PushRangeAsync(IEnumerable<object> collection)
        {
            var stack = GetStack();
            lock (stack)
            {
                foreach (var v in collection)
                {
                    stack.Push(v);
                }
            }
            return ReturnNil();
        }

        public UnaryResult<ConditionalValue<object>> TryPeekAsync()
        {
            var stack = GetStack();
            lock (stack)
            {
                return UnaryResult(new ConditionalValue<object>(stack.TryPeek(out var result), result));
            }
        }

        public UnaryResult<object[]> ToArrayAsync()
        {
            var stack = GetStack();
            lock (stack)
            {
                return UnaryResult(stack.ToArray());
            }
        }
    }

    internal sealed class DistributedStack<T> : IDistributedStack<T>
    {
        readonly IDistributedStackService client;

        public string Key { get; }

        internal DistributedStack(string key, IDistributedStackService client)
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

        public async Task<ConditionalValue<T>> TryPopAsync()
        {
            return (await client.TryPopAsync()).As<T>();
        }

        public async Task PushAsync(T item)
        {
            await client.PushAsync(item!);
        }

        public async Task<ConditionalValue<T>> TryPeekAsync()
        {
            return (await client.TryPeekAsync()).As<T>();
        }

        public async Task<T[]> ToArrayAsync()
        {
            return (await client.ToArrayAsync()).Cast<T>().ToArray();
        }

        public async Task PushRangeAsync(IEnumerable<T> collection)
        {
            await client.PushRangeAsync(collection.Cast<object>());
        }
    }
}