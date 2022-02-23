using DFrame;
using Microsoft.AspNetCore.Builder;

var builder = WebApplication.CreateBuilder(args);
await builder.RunDFrameControllerAsync(opt =>
{
    opt.Title = "foo";
});




//builder.Logging.ClearProviders();
//builder.Logging.SetMinimumLevel(LogLevel.Information);
//builder.Logging.AddZLoggerConsole(options => { });