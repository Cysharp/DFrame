using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.ExceptionServices;
using System.Text;
using System.Threading.Tasks;

namespace DFrame;

public static class DFrameAppHostBuilderExtensions
{
    public static async Task RunDFrameAsync(this IHostBuilder hostBuilder, string[] args, DFrameOptions options)
    {
        var workloadCollection = DFrameWorkloadCollection.FromCurrentAssemblies();

        if (args.Length == 1 && args[0] == "help")
        {
            ShowDFrameAppList(workloadCollection);
            return;
        }

        var errorHolder = new ExceptionHoldLoggerProvider();

        hostBuilder = hostBuilder
            .ConfigureServices(x =>
            {
                x.AddSingleton(options);
                x.AddSingleton(workloadCollection);

                foreach (var item in workloadCollection.All)
                {
                    x.AddTransient(item.WorkloadType);
                }
            })
            .ConfigureLogging(x =>
            {
                x.AddProvider(errorHolder);
            });

        var app = ConsoleApp.CreateFromHostBuilder(hostBuilder, args, x=>
        {
            x.ReplaceToUseSimpleConsoleLogger = false;
        });
        app.AddCommands<DFrameWorkerApp>();
        await app.RunAsync();

        if (errorHolder.Exception != null)
        {
            ExceptionDispatchInfo.Throw(errorHolder.Exception);
        }
    }

    static void ShowDFrameAppList(DFrameWorkloadCollection types)
    {
        Console.WriteLine("Workloads:");
        foreach (var item in types.All)
        {
            Console.WriteLine(item.Name);
        }
    }

    class ExceptionHoldLoggerProvider : ILoggerProvider
    {
        public Exception? Exception { get; set; }

        public ILogger CreateLogger(string categoryName)
        {
            return new Logger(this);
        }

        public void Dispose()
        {
        }

        class Logger : ILogger, IDisposable
        {
            ExceptionHoldLoggerProvider parent;

            public Logger(ExceptionHoldLoggerProvider parent)
            {
                this.parent = parent;
            }

            public IDisposable BeginScope<TState>(TState state)
            {
                return this;
            }

            public void Dispose()
            {
            }

            public bool IsEnabled(LogLevel logLevel)
            {
                return true;
            }

            public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
            {
                if (exception != null)
                {
                    parent.Exception = exception;
                }
            }
        }
    }
}