using DFrame;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddSingleton<DFrame.Controller.IEventHandler, ConsoleController.EventHandler>();
await builder.RunDFrameControllerAsync(opt =>
{
    opt.Title = "foo";
});




//builder.Logging.ClearProviders();
//builder.Logging.SetMinimumLevel(LogLevel.Information);
//builder.Logging.AddZLoggerConsole(options => { });