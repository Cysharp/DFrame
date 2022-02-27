using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Threading.Tasks;

namespace DFrame
{
    public static class DFrameAppHostBuilderExtensions
    {
        public static Task RunDFrameWorkerAsync(this IHostBuilder hostBuilder, string controllerAddress)
        {
            return RunDFrameWorkerAsyncCore(hostBuilder, new DFrameWorkerOptions(controllerAddress), (_, __) => { });
        }

        public static Task RunDFrameWorkerAsync(this IHostBuilder hostBuilder, DFrameWorkerOptions options)
        {
            return RunDFrameWorkerAsyncCore(hostBuilder, options, (_, __) => { });
        }

        public static Task RunDFrameWorkerAsync(this IHostBuilder hostBuilder, Action<DFrameWorkerOptions> configureOptions)
        {
            return RunDFrameWorkerAsyncCore(hostBuilder, new DFrameWorkerOptions(), (_, x) => configureOptions(x));
        }

        public static Task RunDFrameWorkerAsync(this IHostBuilder hostBuilder, Action<HostBuilderContext, DFrameWorkerOptions> configureOptions)
        {
            return RunDFrameWorkerAsyncCore(hostBuilder, new DFrameWorkerOptions(), configureOptions);
        }

        static async Task RunDFrameWorkerAsyncCore(IHostBuilder hostBuilder, DFrameWorkerOptions options, Action<HostBuilderContext, DFrameWorkerOptions> configureOptions)
        {
            hostBuilder = hostBuilder
                .ConfigureServices((hostContext, services) =>
                {
                    configureOptions(hostContext, options);
                    services.AddSingleton(options);
                });

            var app = ConsoleApp.CreateFromHostBuilder(hostBuilder, new string[0], x =>
            {
                // this affects indesirable result so disable auto replace.
                x.ReplaceToUseSimpleConsoleLogger = false;
            });
            app.AddCommands<DFrameWorkerApp>();
            await app.RunAsync();
        }
    }
}