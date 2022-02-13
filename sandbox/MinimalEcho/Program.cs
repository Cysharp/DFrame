using ZLogger;

ThreadPool.SetMinThreads(10000, 10000);

var builder = WebApplication.CreateBuilder(args);

builder.Logging.ClearProviders();
builder.Logging.AddZLoggerConsole();

var app = builder.Build();

var logger = app.Services.GetRequiredService<ILogger<Program>>();
int i = 0;
app.MapGet("/", () =>
{
    logger.ZLogInformation("Count:{0}", Interlocked.Increment(ref i));
    return "ok";
});

app.Run();