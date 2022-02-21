using DFrame;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.Logging;
using ZLogger;

var builder = WebApplication.CreateBuilder(args);

builder.Logging.ClearProviders();
builder.Logging.SetMinimumLevel(LogLevel.Information);
builder.Logging.AddZLoggerConsole(options => { });

await builder.RunDFrameControllerAsync();


//// gRPC and MagicOnion
//builder.Services.AddGrpc();
//builder.Services.AddMagicOnion(x =>
//{
//    // Should use same options between DFrame.Controller(this) and DFrame.Worker
//    x.SerializerOptions = MessagePackSerializerOptions.Standard;
//});
//builder.Services.AddSingleton<IMagicOnionLogger, MagicOnionLogToLogger>();

//// Blazor
//builder.Services.AddRazorPages(x =>
//{
//    //RazorPagesOptions opt;


//}).ConfigureApplicationPartManager(manager =>
//{
//    //x.ApplicationParts.Add(
//    var assembly = typeof(DFrame.Controller.ControllerHub).Assembly;
//    var assemblyPart = new CompiledRazorAssemblyPart(assembly);
//    manager.ApplicationParts.Add(assemblyPart);
//});



//builder.Services.AddServerSideBlazor(x =>
//{
//    //x.RootComponents
//});

//// Logging
//builder.Logging.ClearProviders();
//builder.Logging.SetMinimumLevel(LogLevel.Trace);
//builder.Logging.AddZLoggerConsole(options =>
//{

//});

//// Setup Dframe options
//builder.Services.TryAddSingleton<DFrameControllerExecutionEngine>();
//builder.Services.TryAddSingleton<LogRouter>();
//builder.Services.AddSingleton<ILoggerProvider, RoutingLoggerProvider>();
//// If user sets custom provdier, use it.
//builder.Services.TryAddSingleton<IExecutionResultHistoryProvider, InMemoryExecutionResultHistoryProvider>();

//builder.Services.AddMessagePipe();

//builder.Services.AddScoped<LocalStorageAccessor>();

//var app = builder.Build();

//// Configure the HTTP request pipeline.
//if (!app.Environment.IsDevelopment())
//{
//    app.UseExceptionHandler("/Error");
//}


//app.UseStaticFiles();
//app.UseRouting();

//app.MapBlazorHub(x =>
//{

//});

//app.MapFallbackToPage("/_Host");

//// MagicOnion Routing
//app.MapMagicOnionService();

//DisplayConfiguration(app);

//app.Run();


//static void DisplayConfiguration(WebApplication app)
//{
//    var config = app.Services.GetRequiredService<IConfiguration>();

//    var http1Endpoint = config.GetSection("Kestrel:Endpoints:Http:Url");
//    if (http1Endpoint != null)
//    {
//        app.Logger.ZLogInformation("Hosting DFrame.Controller on {0}. You can open this address by browser.", http1Endpoint.Value);
//    }

//    var gprcEndpoint = config.GetSection("Kestrel:Endpoints:Grpc:Url");
//    if (gprcEndpoint != null)
//    {
//        app.Logger.ZLogInformation("Hosting MagicOnion(gRPC) address on {0}. Setup this address to DFrameWorkerOptions.ControllerAddress.", gprcEndpoint.Value);
//    }
//}