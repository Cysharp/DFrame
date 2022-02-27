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
    public static void Run(int portWeb, int portListenWorker, string? controllerAddress = null, bool useHttps = false)
    {
        new DFrameAppBuilder(portWeb, portListenWorker, useHttps).Run(controllerAddress);
    }

    public static async Task RunAsync(int portWeb, int portListenWorker, string? controllerAddress = null, bool useHttps = false)
    {
        await new DFrameAppBuilder(portWeb, portListenWorker, useHttps).RunAsync(controllerAddress);
    }

    public static DFrameAppBuilder CreateBuilder(int portWeb, int portListenWorker, bool useHttps = false)
    {
        return new DFrameAppBuilder(portWeb, portListenWorker, useHttps);
    }
}

public class DFrameAppBuilder
{
    Action<WebHostBuilderContext, DFrameControllerOptions> configureController;
    Action<HostBuilderContext, DFrameWorkerOptions> configureWorker;

    string workerListenAddress;

    public WebApplicationBuilder ControllerBuilder { get; }
    public IHostBuilder WorkerBuilder { get; }

    internal DFrameAppBuilder(int portWeb, int portListenWorker, bool useHttps)
    {
        this.workerListenAddress = ((useHttps) ? "https" : "http") + "://localhost:" + portListenWorker;

        configureController = (_, __) => { };
        configureWorker = (_, options) => options.ControllerAddress = workerListenAddress;

        var args = Environment.GetCommandLineArgs();

        var controllerBuilder = WebApplication.CreateBuilder(args);
        controllerBuilder.WebHost
            .UseKestrel(x =>
            {
                x.Listen(IPAddress.Any, portWeb, option =>
                {
                    option.Protocols = HttpProtocols.Http1;
                    if (useHttps)
                    {
                        option.UseHttps();
                    }
                });
                x.Listen(IPAddress.Any, portListenWorker, option =>
                {
                    option.Protocols = HttpProtocols.Http2;
                    if (useHttps)
                    {
                        option.UseHttps();
                    }
                });
            });

        ControllerBuilder = controllerBuilder;
        WorkerBuilder = Host.CreateDefaultBuilder(args);
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

    public void ConfigureController(Action<DFrameControllerOptions> configureController)
    {
        this.configureController = (_, options) => configureController(options);
    }

    public void ConfigureController(Action<WebHostBuilderContext, DFrameControllerOptions> configureController)
    {
        this.configureController = configureController;
    }

    public void ConfigureWorker(Action<HostBuilderContext, DFrameWorkerOptions> configureWorker)
    {
        this.configureWorker = (ctx, options) =>
        {
            options.ControllerAddress = workerListenAddress;
            configureWorker(ctx, options);
        };
    }

    public void ConfigureWorker(Action<DFrameWorkerOptions> configureWorker)
    {
        this.configureWorker = (_, options) =>
        {
            options.ControllerAddress = workerListenAddress;
            configureWorker(options);
        };
    }

    public void Run(string? controllerAddress = null)
    {
        RunAsync(controllerAddress).GetAwaiter().GetResult();
    }

    public async Task RunAsync(string? controllerAddress = null)
    {
        var controller = RunControllerAsync();
        var worker = RunWorkerAsync(controllerAddress);
        await Task.WhenAll(controller, worker);
    }

    public async Task RunControllerAsync()
    {
        await ControllerBuilder.RunDFrameControllerAsync(configureController);
    }

    public async Task RunWorkerAsync(string? controllerAddress = null)
    {
        if (controllerAddress != null)
        {
            this.workerListenAddress = controllerAddress;
        }

        await WorkerBuilder.RunDFrameWorkerAsync(configureWorker);
    }
}