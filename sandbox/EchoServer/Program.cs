using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using System.Text;
using System.Threading.Tasks;
using ZLogger;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Routing;
using System.Buffers;
using Microsoft.AspNetCore.Http;

namespace EchoServer
{
    class Program
    {
        static async Task Main(string[] args)
        {
            await Host.CreateDefaultBuilder(args)
                .ConfigureWebHost(builder =>
                {
                    builder
                    .UseKestrel()
                    .UseStartup<Startup>();
                })
                .ConfigureLogging(x=>
                {
                    x.ClearProviders();
                    x.SetMinimumLevel(LogLevel.Trace);
                    x.AddZLoggerConsole();
                })
                .RunConsoleAsync();

        }
    }

    class Startup
    {
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddRouting(options => options.LowercaseUrls = true);
        }
        public void Configure(IApplicationBuilder app, ILogger<Startup> logger)
        {
            var hello = Encoding.UTF8.GetBytes("hello");
            var world = Encoding.UTF8.GetBytes("world");
            var login = Encoding.UTF8.GetBytes("login");
            var d = Encoding.UTF8.GetBytes("default");

            app.UseRouting();
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapGet("/", async x => await x.Response.BodyWriter.WriteAsync(d));
                endpoints.MapGet("/hello", async x => await x.Response.BodyWriter.WriteAsync(hello));
                endpoints.MapGet("/world", async x => await x.Response.BodyWriter.WriteAsync(world));
                endpoints.MapGet("/item/{id?}", async x =>
                {
                    var id = x.Request.Query["id"];
                    await x.Response.WriteAsync($"item {id}");
                });
                endpoints.MapPost("/login", async x => await x.Response.BodyWriter.WriteAsync(login));
            });
            //app.Run(async x => await x.Response.BodyWriter.WriteAsync(d));
        }
    }
}