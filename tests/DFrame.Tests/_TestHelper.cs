#pragma warning disable CS1998

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit.Abstractions;

namespace DFrame.Tests
{
    public static class TestHelper
    {
        static int port = 10000;

        public static async Task<T> RunDFrameAsync<T>(ITestOutputHelper helper, string[] args)
        {
            var box = new ResultBox<T>();
            var task = Host.CreateDefaultBuilder()
                .ConfigureServices(x =>
                {
                    x.AddSingleton(_ => box);
                })
                .ConfigureLogging(x =>
                {
                    x.ClearProviders();
                    x.SetMinimumLevel(LogLevel.Trace);
                    x.AddProvider(new TestOutputLoggerProvider(helper));
                })
                .RunDFrameAsync(args, new DFrameOptions("localhost", Interlocked.Increment(ref port))
                {
                    ConfigureInnerHostLogging = logging =>
                    {
                        logging.ClearProviders();
                        logging.SetMinimumLevel(LogLevel.Trace);
                        logging.AddProvider(new TestOutputLoggerProvider(helper));
                    },
                    OnExecuteResult = (results, opt, scenario)=>
                    {
                        var error = results.FirstOrDefault(x => x.HasError);
                        if (error != null)
                        {
                            throw new Exception("Worker Execution has errors. " + error.ErrorMessage);
                        }
                    }
                });
            await task;
            return box.Value;
        }

        public static Task<TResult> RunDFrameAsync<TTestType, TResult>(ITestOutputHelper helper)
        {
            var args = $"batch -processCount 1 -workerPerProcess 1 -executePerWorker 1 -workerName {typeof(TTestType).Name}".Split(' ');
            return RunDFrameAsync<TResult>(helper, args);
        }
    }


    public class ResultBox<T>
    {
        public T Value;
    }

    public class TestOutputLoggerProvider : ILoggerProvider
    {
        readonly ITestOutputHelper helper;

        public TestOutputLoggerProvider(ITestOutputHelper helper)
        {
            this.helper = helper;
        }

        public ILogger CreateLogger(string categoryName)
        {
            return new TestOutputLogger(helper);
        }

        public void Dispose()
        {
        }
    }

    public class TestOutputLogger : ILogger
    {
        readonly ITestOutputHelper helper;

        public TestOutputLogger(ITestOutputHelper helper)
        {
            this.helper = helper;
        }

        public System.IDisposable BeginScope<TState>(TState state)
        {
            return NullDisposable.Instance;
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            return true;
        }

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, System.Exception exception, System.Func<TState, System.Exception, string> formatter)
        {
            helper.WriteLine($"[{logLevel.ToString()}]" + formatter.Invoke(state, exception));
            if (exception != null)
            {
                helper.WriteLine(exception.ToString());
            }
        }

        class NullDisposable : IDisposable
        {
            public static readonly IDisposable Instance = new NullDisposable();

            public void Dispose()
            {
            }
        }
    }
}
