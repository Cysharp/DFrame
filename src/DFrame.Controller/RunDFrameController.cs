using DFrame.Controller;
using DFrame.Internal;
using MagicOnion.Serialization;
using MagicOnion.Server;
using MagicOnion.Server.Diagnostics;
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
        appBuilder.Services.AddGrpc();
        appBuilder.Services.AddMagicOnion(x =>
        {
            // Should use same options between DFrame.Controller(this) and DFrame.Worker
            x.MessageSerializer = MessagePackMagicOnionSerializerProvider.Default;
        });
        appBuilder.Services.AddSingleton<IMagicOnionLogger, MagicOnionLogToLogger>();

        appBuilder.Services.AddRazorPages()
            .ConfigureApplicationPartManager(manager =>
            {
                // import libraries razor pages
                var assembly = typeof(DFrameControllerWebApplicationBuilderExtensions).Assembly;
                var assemblyPart = new CompiledRazorAssemblyPart(assembly);
                manager.ApplicationParts.Add(assemblyPart);
            });

        appBuilder.Services.AddServerSideBlazor();

        // DFrame Options
        appBuilder.Services.TryAddSingleton<DFrameControllerExecutionEngine>();
        appBuilder.Services.TryAddSingleton<DFrameControllerLogBuffer>();
        appBuilder.Services.AddSingleton<ILoggerProvider, DFrameControllerLoggerProvider>();
        appBuilder.Services.AddScoped<LocalStorageAccessor>();
#pragma warning disable ASP0012 // Suggest using builder.Services over Host.ConfigureServices or WebHost.ConfigureServices
        appBuilder.WebHost.ConfigureServices((ctx, services) => configureOptions(ctx, options));
#pragma warning restore ASP0012 // Suggest using builder.Services over Host.ConfigureServices or WebHost.ConfigureServices
        appBuilder.Services.AddSingleton(options);

        // If user sets custom provider, use it.
        appBuilder.Services.TryAddSingleton<IExecutionResultHistoryProvider, InMemoryExecutionResultHistoryProvider>();

        appBuilder.Services.AddMessagePipe();

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
