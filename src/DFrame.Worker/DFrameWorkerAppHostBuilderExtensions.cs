using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace DFrame;

public static class DFrameAppHostBuilderExtensions
{
    public static Task RunDFrameAsync(this IHostBuilder hostBuilder, string[] args, DFrameWorkerOptions options)
    {
        return RunDFrameAsyncCore(hostBuilder, args, options, (_, __) => { });
    }

    public static Task RunDFrameAsync(this IHostBuilder hostBuilder, string[] args, Action<DFrameWorkerOptions> configureOptions)
    {
        return RunDFrameAsyncCore(hostBuilder, args, new DFrameWorkerOptions(), (_, x) => configureOptions(x));
    }

    public static Task RunDFrameAsync(this IHostBuilder hostBuilder, string[] args, Action<HostBuilderContext, DFrameWorkerOptions> configureOptions)
    {
        return RunDFrameAsyncCore(hostBuilder, args, new DFrameWorkerOptions(), configureOptions);
    }

    static async Task RunDFrameAsyncCore(IHostBuilder hostBuilder, string[] args, DFrameWorkerOptions options, Action<HostBuilderContext, DFrameWorkerOptions> configureOptions)
    {
        hostBuilder = hostBuilder
            .ConfigureServices((hostContext, services) =>
            {
                configureOptions(hostContext, options);
                services.AddSingleton(options);
            });

        var app = ConsoleApp.CreateFromHostBuilder(hostBuilder, args, x =>
        {
            // this affects indesirable result so disable auto replace.
            x.ReplaceToUseSimpleConsoleLogger = false;
        });
        app.AddCommands<DFrameWorkerApp>();
        await app.RunAsync();
    }
}