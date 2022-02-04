using DFrame.Controller;
using Grpc.Core;
using MagicOnion.Server;
using MagicOnion.Server.Hubs;
using MessagePack;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection.Extensions;
using ZLogger;

var builder = WebApplication.CreateBuilder(args);

// gRPC and MagicOnion
builder.Services.AddGrpc();
builder.Services.AddMagicOnion(x =>
{
    x.IsReturnExceptionStackTraceInErrorDetail = true;

    // Should use same options between DFrame.Controller(this) and DFrame.Worker
    x.SerializerOptions = MessagePackSerializerOptions.Standard;
});
builder.Services.AddSingleton<IMagicOnionLogger, MagicOnionLogToLogger>();

// Blazor
builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();

// Logging
builder.Logging.ClearProviders();
builder.Logging.SetMinimumLevel(LogLevel.Trace);
builder.Logging.AddZLoggerConsole(options =>
{

});

// Setup Dframe options
builder.Services.TryAddSingleton<WorkerConnectionGroupContext>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
}


app.UseStaticFiles();
app.UseRouting();

app.MapBlazorHub();
app.MapFallbackToPage("/_Host");

// MagicOnion Routing
app.MapMagicOnionService();

app.Run();

public class ErrorLoggingFilter : MagicOnionFilterAttribute
{
    readonly ILogger<ErrorLoggingFilter> logger;

    public ErrorLoggingFilter(ILogger<ErrorLoggingFilter> logger)
    {
        this.logger = logger;
    }

    public override ValueTask Invoke(ServiceContext context, Func<ServiceContext, ValueTask> next)
    {
        try
        {
            return next(context);
        }
        catch (Exception ex)
        {
            logger.LogError(exception: ex, "Error occured in MagicOnion");
            throw;
        }
    }
}