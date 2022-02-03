using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace DFrame;

public static class DFrameAppHostBuilderExtensions
{
    // TODO: ConfigureOptions
    public static async Task RunDFrameAsync(this IHostBuilder hostBuilder, string[] args, DFrameOptions options)
    {
        hostBuilder = hostBuilder
            .ConfigureServices(x =>
            {
                x.AddSingleton(options);
            });

        var app = ConsoleApp.CreateFromHostBuilder(hostBuilder, args, x =>
        {
            x.ReplaceToUseSimpleConsoleLogger = false;
        });
        app.AddCommands<DFrameWorkerApp>();
        await app.RunAsync();
    }
}