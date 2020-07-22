using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using System.Text;
using System.Threading.Tasks;

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
                .RunConsoleAsync();

        }
    }

    class Startup
    {
        public void Configure(IApplicationBuilder app)
        {
            var hello = Encoding.UTF8.GetBytes("hello");

            app.Run(async x =>
            {
                await x.Response.BodyWriter.WriteAsync(hello);
            });
        }
    }
}