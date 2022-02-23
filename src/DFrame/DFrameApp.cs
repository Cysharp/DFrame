using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Net;

namespace DFrame;

public static class DFrameApp
{
    public static void Run(string hostAddress, bool useHttps = false)
    {
        RunAsync(hostAddress, useHttps).GetAwaiter().GetResult();
    }

    public static async Task RunAsync(string hostAddress, bool useHttps = false)
    {
        await new DFrameAppBuilder(hostAddress, useHttps).RunAsync();
    }

    public static DFrameAppBuilder CreateBuilder(string hostAddress, bool useHttps = false)
    {
        return new DFrameAppBuilder(hostAddress, useHttps);
    }
}

public class DFrameAppBuilder
{
    Action<WebHostBuilderContext, DFrameControllerOptions> configureController;
    Action<HostBuilderContext, DFrameWorkerOptions> configureWorker;

    public string Http1HostAddress { get; }
    public string Http2HostAddress { get; }

    public WebApplicationBuilder ControllerBuilder { get; }
    public IHostBuilder WorkerBuilder { get; }

    internal DFrameAppBuilder(string hostAddress, bool useHttps)
    {
        // address == HTTP/1 port.
        // gRPC address == port + 1

        var last = hostAddress.LastIndexOf(':');
        var portString = hostAddress.Substring(last + 1);
        if (!int.TryParse(portString, out var port))
        {
            throw new ArgumentException($"Can not parse port. address:{hostAddress}");
        }
        var gRpcAddress = hostAddress.Substring(0, last) + ":" + (port + 1);

        var args = Environment.GetCommandLineArgs();

        var controllerBuilder = WebApplication.CreateBuilder(args);
        controllerBuilder.WebHost
            .UseKestrel(x =>
            {
                x.Listen(IPAddress.Any, port, option =>
                {
                    option.Protocols = HttpProtocols.Http1;
                    if (useHttps)
                    {
                        option.UseHttps();
                    }
                });
                x.Listen(IPAddress.Any, port + 1, option =>
                {
                    option.Protocols = HttpProtocols.Http2;
                    if (useHttps)
                    {
                        option.UseHttps();
                    }
                });
            });

        configureController = (_, __) => { };
        configureWorker = (_, options) => options.ControllerAddress = gRpcAddress;

        ControllerBuilder = controllerBuilder;
        WorkerBuilder = Host.CreateDefaultBuilder(args);
        Http1HostAddress = hostAddress;
        Http2HostAddress = gRpcAddress;
    }

    public void ConfigureServices(Action<IServiceCollection> configureDelegate)
    {
        ControllerBuilder.Host.ConfigureServices(configureDelegate);
        WorkerBuilder.ConfigureServices(configureDelegate);
    }

    public void ConfigureServices(Action<HostBuilderContext, IServiceCollection> configureDelegate)
    {
        ControllerBuilder.Host.ConfigureServices(configureDelegate);
        WorkerBuilder.ConfigureServices(configureDelegate);
    }

    public void ConfigureAppConfiguration(Action<IConfigurationBuilder> configureDelegate)
    {
        ControllerBuilder.Host.ConfigureAppConfiguration(configureDelegate);
        WorkerBuilder.ConfigureAppConfiguration(configureDelegate);
    }

    public void ConfigureAppConfiguration(Action<HostBuilderContext, IConfigurationBuilder> configureDelegate)
    {
        ControllerBuilder.Host.ConfigureAppConfiguration(configureDelegate);
        WorkerBuilder.ConfigureAppConfiguration(configureDelegate);
    }

    public void ConfigureContainer<TContainerBuilder>(Action<TContainerBuilder> configureDelegate)
    {
        ControllerBuilder.Host.ConfigureContainer(configureDelegate);
        WorkerBuilder.ConfigureContainer(configureDelegate);
    }

    public void ConfigureContainer<TContainerBuilder>(Action<HostBuilderContext, TContainerBuilder> configureDelegate)
    {
        ControllerBuilder.Host.ConfigureContainer(configureDelegate);
        WorkerBuilder.ConfigureContainer(configureDelegate);
    }

    public void ConfigureHostConfiguration(Action<IConfigurationBuilder> configureDelegate)
    {
        ControllerBuilder.Host.ConfigureHostConfiguration(configureDelegate);
        WorkerBuilder.ConfigureHostConfiguration(configureDelegate);
    }

    public void UseServiceProviderFactory<TContainerBuilder>(IServiceProviderFactory<TContainerBuilder> factory)
        where TContainerBuilder : notnull
    {
        ControllerBuilder.Host.UseServiceProviderFactory(factory);
        WorkerBuilder.UseServiceProviderFactory(factory);
    }

    public void UseServiceProviderFactory<TContainerBuilder>(Func<HostBuilderContext, IServiceProviderFactory<TContainerBuilder>> factory)
        where TContainerBuilder : notnull
    {
        ControllerBuilder.Host.UseServiceProviderFactory(factory);
        WorkerBuilder.UseServiceProviderFactory(factory);
    }

    public void ConfigureLogging(Action<ILoggingBuilder> configureDelegate)
    {
        ControllerBuilder.Host.ConfigureLogging(configureDelegate);
        WorkerBuilder.ConfigureLogging(configureDelegate);
    }

    public void ConfigureLogging(Action<HostBuilderContext, ILoggingBuilder> configureDelegate)
    {
        ControllerBuilder.Host.ConfigureLogging(configureDelegate);
        WorkerBuilder.ConfigureLogging(configureDelegate);
    }

    public void ConfigureController(Action<WebHostBuilderContext, DFrameControllerOptions> configureController)
    {
        this.configureController = configureController;
    }

    public void ConfigureWorker(Action<HostBuilderContext, DFrameWorkerOptions> configureWorker)
    {
        this.configureWorker = (ctx, options) =>
        {
            options.ControllerAddress = Http2HostAddress;
            configureWorker(ctx, options);
        };
    }

    public async Task RunAsync()
    {
        var controller = ControllerBuilder.RunDFrameControllerAsync(configureController);
        var worker = WorkerBuilder.RunDFrameAsync(configureWorker);
        await Task.WhenAll(controller, worker);
    }
}