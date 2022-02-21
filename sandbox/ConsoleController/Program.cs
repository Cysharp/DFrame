using DFrame;
using Microsoft.AspNetCore.Builder;

var builder = WebApplication.CreateBuilder(args);
await builder.RunDFrameControllerAsync();


//builder.Logging.ClearProviders();
//builder.Logging.SetMinimumLevel(LogLevel.Information);
//builder.Logging.AddZLoggerConsole(options => { });