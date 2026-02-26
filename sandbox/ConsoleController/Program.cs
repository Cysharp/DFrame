using DFrame;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddMessagePipe();
builder.Services.AddHostedService<ConsoleController.EventHandler>();

await builder.RunDFrameControllerAsync(opt =>
{
    opt.Title = "foo";
});
