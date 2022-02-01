using DFrame.Controller;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection.Extensions;
using ZLogger;

var builder = WebApplication.CreateBuilder(args);

// gRPC and MagicOnion
builder.Services.AddGrpc();
builder.Services.AddMagicOnion();

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