using System;
using System.Threading.Tasks;
using EchoMagicOnion.Shared;
using MagicOnion;
using MagicOnion.Server;
using MagicOnion.Server.Hubs;
using MessagePack;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ZLogger;

namespace EchoMagicOnion
{
    class Program
    {
        static async Task Main(string[] args)
        {
            //GrpcEnvironment.SetLogger(new Grpc.Core.Logging.ConsoleLogger());

            await Host.CreateDefaultBuilder()
                .ConfigureLogging((context, logging) =>
                {
                    logging.ClearProviders();
                    logging.SetMinimumLevel(LogLevel.Trace);
                    logging.AddZLoggerConsole();
                })
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder
                        .UseKestrel(options =>
                        {
                            // WORKAROUND: Accept HTTP/2 only to allow insecure HTTP/2 connections during development.
                            options.ConfigureEndpointDefaults(endpointOptions =>
                            {
                                endpointOptions.Protocols = HttpProtocols.Http2;
                            });
                        })
                        .UseStartup<Startup>();
                })
                .Build()
                .RunAsync();
        }
    }

    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddGrpc(); // MagicOnion depends on ASP.NET Core gRPC service.
            services.AddMagicOnion();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }
            //app.UseHttpsRedirection();

            app.UseRouting();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapMagicOnionService();
            });
        }
    }

    public class EchoService : ServiceBase<IEchoService>, IEchoService
    {
        public UnaryResult<Nil> Echo(string message)
        {
            return UnaryResult(Nil.Default);
        }
    }

    public class EchoHub : StreamingHubBase<IEchoHub, IEchoHubReceiver>, IEchoHub
    {
        private IEchoHubReceiver _broadcaster;
        protected override async ValueTask OnConnecting()
        {
            var group = await Group.AddAsync("global-masterhub-group");
            _broadcaster = group.CreateBroadcaster<IEchoHubReceiver>();
        }

        public Task<MessageResponse> EchoAsync(string message)
        {
            var response = new MessageResponse { Message = message };

            return Task.FromResult(response);
        }

        public Task<MessageResponse> EchoBroadcastAsync(string message)
        {
            var response = new MessageResponse { Message = message };

            // broadcast to all client
            _broadcaster.OnSend(response);

            return Task.FromResult(response);
        }
    }
}
