using DFrame.Controller;
using DFrame.Internal;
using MagicOnion.Server;
using MessagePack;
using MessagePipe;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace DFrame;

public static class DFrameControllerWebApplicationBuilderExtensions
{
    public static Task RunDFrameControllerAsync(this WebApplicationBuilder appBuilder)
    {
        return RunDFrameControllerAsync(appBuilder, new DFrameControllerOptions(), (_, __) => { });
    }

    public static Task RunDFrameControllerAsync(this WebApplicationBuilder appBuilder, DFrameControllerOptions options)
    {
        return RunDFrameControllerAsync(appBuilder, options, (_, __) => { });
    }

    public static Task RunDFrameControllerAsync(this WebApplicationBuilder appBuilder, Action<DFrameControllerOptions> configureOptions)
    {
        return RunDFrameControllerAsync(appBuilder, new DFrameControllerOptions(), (_, x) => configureOptions(x));
    }

    public static Task RunDFrameControllerAsync(this WebApplicationBuilder appBuilder, Action<WebHostBuilderContext, DFrameControllerOptions> configureOptions)
    {
        return RunDFrameControllerAsync(appBuilder, new DFrameControllerOptions(), configureOptions);
    }

    static async Task RunDFrameControllerAsync(WebApplicationBuilder appBuilder, DFrameControllerOptions options, Action<WebHostBuilderContext, DFrameControllerOptions> configureOptions)
    {
        appBuilder.WebHost.ConfigureServices((WebHostBuilderContext ctx, IServiceCollection services) =>
        {
            services.AddGrpc();
            services.AddMagicOnion(x =>
            {
                // Should use same options between DFrame.Controller(this) and DFrame.Worker
                x.SerializerOptions = MessagePackSerializerOptions.Standard;
            });
            services.AddSingleton<IMagicOnionLogger, MagicOnionLogToLogger>();

            services.AddRazorPages()
                .ConfigureApplicationPartManager(manager =>
                {
                    // import libraries razor pages
                    var assembly = typeof(DFrameControllerWebApplicationBuilderExtensions).Assembly;
                    var assemblyPart = new CompiledRazorAssemblyPart(assembly);
                    manager.ApplicationParts.Add(assemblyPart);
                });

            services.AddServerSideBlazor();

            // DFrame Options
            services.TryAddSingleton<DFrameControllerExecutionEngine>();
            services.TryAddSingleton<DFrameControllerLogBuffer>();
            services.AddSingleton<ILoggerProvider, DFrameControllerLoggerProvider>();
            services.AddScoped<LocalStorageAccessor>();
            configureOptions(ctx, options);
            services.AddSingleton(options);

            // If user sets custom provdier, use it.
            services.TryAddSingleton<IExecutionResultHistoryProvider, InMemoryExecutionResultHistoryProvider>();

            services.AddMessagePipe();
        });

        var app = appBuilder.Build();

        app.UseStaticFiles();
        app.UseRouting();

        app.MapBlazorHub();
        app.MapFallbackToPage("/_Host");

        app.MapMagicOnionService();

        DisplayConfiguration(app);

        if (!options.DisableRestApi)
        {
            RestApi.RegisterRestApi(app);
        }

        await app.RunAsync();
    }

    static void DisplayConfiguration(WebApplication app)
    {
        var config = app.Services.GetRequiredService<IConfiguration>();

        var http1Endpoint = config.GetSection("Kestrel:Endpoints:Http:Url");
        if (http1Endpoint != null && http1Endpoint.Value != null)
        {
            app.Logger.LogInformation($"Hosting DFrame.Controller on {http1Endpoint.Value}. You can open this address by browser.");
        }

        var gprcEndpoint = config.GetSection("Kestrel:Endpoints:Grpc:Url");
        if (gprcEndpoint != null && gprcEndpoint.Value != null)
        {
            app.Logger.LogInformation($"Hosting MagicOnion(gRPC) address on {gprcEndpoint.Value}. Setup this address to DFrameWorkerOptions.ControllerAddress.");
        }
    }

}
