using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace DFrame
{
    internal static class Shims
    {
        internal static bool TryAdd(this Dictionary<string, DFrameWorkloadTypeInfo> dict, string key, DFrameWorkloadTypeInfo value)
        {
            if (!dict.ContainsKey(key))
            {
                dict.Add(key, value);
                return true;
            }
            return false;
        }

        internal static async Task<T> WaitAsync<T>(this Task<T> task, TimeSpan timeout)
        {
            var timeoutCancellation = new CancellationTokenSource();
            var timeoutTask = Task.Delay(timeout, timeoutCancellation.Token);
            var resultTask = await Task.WhenAny(task, timeoutTask);
            if (resultTask == timeoutTask)
            {
                timeoutCancellation.Dispose();
                throw new TimeoutException();
            }
            else
            {
                timeoutCancellation.Cancel();
                timeoutCancellation.Dispose();
                return task.Result;
            }
        }

        internal static async Task<T> WaitAsync<T>(this Task<T> task, CancellationToken cancellationToken)
        {
            var tcs = new TaskCompletionSource<object>();
            var registration = cancellationToken.Register(state =>
            {
                var t = (TaskCompletionSource<object>)state;
                t.TrySetResult(null);
            }, tcs, false);

            var resultTask = await Task.WhenAny(task, tcs.Task);
            if (resultTask == tcs.Task)
            {
                registration.Dispose();
                throw new TimeoutException();
            }
            else
            {
                registration.Dispose();
                return task.Result;
            }
        }

        internal static async Task WaitAsync(this Task task, TimeSpan timeout, CancellationToken cancellationToken)
        {
            var timeoutCancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            var timeoutTask = Task.Delay(timeout, timeoutCancellation.Token);
            var resultTask = await Task.WhenAny(task, timeoutTask);
            if (resultTask == timeoutTask)
            {
                timeoutCancellation.Dispose();
                throw new TimeoutException();
            }
            else
            {
                timeoutCancellation.Cancel();
                timeoutCancellation.Dispose();
                return;
            }
        }
    }

    internal static class _Random
    {
        [ThreadStatic]
        static Random random;

        public static Random Shared
        {
            get
            {
                if (random == null)
                {
                    using (var rng = new RNGCryptoServiceProvider())
                    {
                        var buffer = new byte[sizeof(int)];
                        rng.GetBytes(buffer);
                        var seed = BitConverter.ToInt32(buffer, 0);
                        random = new Random(seed);
                    }
                }
                return random;
            }
        }
    }

        internal delegate object ObjectFactory(IServiceProvider serviceProvider, object[] arguments);

    internal static class ActivatorUtilities
    {
        internal static ObjectFactory CreateFactory(Type type, Type[] _)
        {
            return (__, args) => Activator.CreateInstance(type, args);
        }
    }

    internal class IServiceProviderIsService
    {
        public bool IsService(Type type) => true;
    }

    public class ConsoleAppBase
    {
        protected ConsoleAppContext Context { get; set; } = new ConsoleAppContext();
    }

    public class ConsoleAppContext
    {
        public CancellationToken CancellationToken { get; set; }
    }

    internal class RootCommandAttribute : Attribute
    {
    }

    internal class ILogger<T>
    {
        public void LogInformation(string message)
        {
            UnityEngine.Debug.Log(message);
        }
        public void LogError(Exception ex, string message)
        {
            UnityEngine.Debug.LogException(ex);
        }
    }
}

namespace System.Diagnostics.CodeAnalysis
{
    [AttributeUsage(AttributeTargets.Parameter, AllowMultiple = false)]
    internal sealed class NotNullWhenAttribute : Attribute
    {
        public bool ReturnValue
        {
            get;
        }

        public NotNullWhenAttribute(bool returnValue)
        {
            ReturnValue = returnValue;
        }
    }
}

namespace Microsoft.Extensions.DependencyInjection
{

}

namespace Microsoft.Extensions.Logging
{

}

namespace Grpc.Net.Client
{

}