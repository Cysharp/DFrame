using Microsoft.AspNetCore.Builder;
using Cysharp.Text;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using System.Text;
using System.Threading.Tasks;
using ZLogger;
using System.Threading;

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
        public void Configure(IApplicationBuilder app, ILogger<Startup> logger)
        {
            var hello = Encoding.UTF8.GetBytes("hello");

            app.Run(async x =>
            {
                await x.Response.BodyWriter.WriteAsync(hello);
            });
        }
    }
}