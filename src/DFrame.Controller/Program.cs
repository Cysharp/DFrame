using DFrame.Controller;
using MagicOnion.Server;
using MessagePack;
using Microsoft.Extensions.DependencyInjection.Extensions;
using ObservableCollections;
using ZLogger;

var builder = WebApplication.CreateBuilder(args);

// gRPC and MagicOnion
builder.Services.AddGrpc();
builder.Services.AddMagicOnion(x =>
{
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
builder.Services.TryAddSingleton<DFrameControllerExecutionEngine>();
builder.Services.TryAddSingleton<LogRouter>();
builder.Services.AddSingleton<ILoggerProvider, RoutingLoggerProvider>();

var app = builder.Build();

GlobalServiceProvider.Instance = app.Services;

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

DisplayConfiguration(app);

app.Run();


static void DisplayConfiguration(WebApplication app)
{
    var config = app.Services.GetRequiredService<IConfiguration>();

    var http1Endpoint = config.GetSection("Kestrel:Endpoints:Http:Url");
    if (http1Endpoint != null)
    {
        app.Logger.ZLogInformation("Hosting DFrame.Controller on {0}. You can open this address by browser.", http1Endpoint.Value);
    }

    var gprcEndpoint = config.GetSection("Kestrel:Endpoints:Grpc:Url");
    if (gprcEndpoint != null)
    {
        app.Logger.ZLogInformation("Hosting MagicOnion(gRPC) address on {0}. Setup this address to DFrameWorkerOptions.ControllerAddress.", gprcEndpoint.Value);
    }
}