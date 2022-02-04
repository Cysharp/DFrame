using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace DFrame;

public static class DFrameAppHostBuilderExtensions
{
    public static Task RunDFrameAsync(this IHostBuilder hostBuilder, string[] args, DFrameOptions options)
    {
        return RunDFrameAsyncCore(hostBuilder, args, options, (_, __) => { });
    }

    public static Task RunDFrameAsync(this IHostBuilder hostBuilder, string[] args, Action<DFrameOptions> configureOptions)
    {
        return RunDFrameAsyncCore(hostBuilder, args, new DFrameOptions(), (_, x) => configureOptions(x));
    }

    public static Task RunDFrameAsync(this IHostBuilder hostBuilder, string[] args, Action<HostBuilderContext, DFrameOptions> configureOptions)
    {
        return RunDFrameAsyncCore(hostBuilder, args, new DFrameOptions(), configureOptions);
    }

    static async Task RunDFrameAsyncCore(IHostBuilder hostBuilder, string[] args, DFrameOptions options, Action<HostBuilderContext, DFrameOptions> configureOptions)
    {
        hostBuilder = hostBuilder
            .ConfigureServices((hostContext, services) =>
            {
                configureOptions(hostContext, options);
                services.AddSingleton(options);
            });

        var app = ConsoleApp.CreateFromHostBuilder(hostBuilder, args, x =>
        {
            x.ReplaceToUseSimpleConsoleLogger = false;
        });
        app.AddCommands<DFrameWorkerApp>();
        await app.RunAsync();
    }
}