using ZLogger;

ThreadPool.SetMinThreads(10000, 10000);

var builder = WebApplication.CreateBuilder(args);

builder.Logging.ClearProviders();
builder.Logging.AddZLoggerConsole();

var app = builder.Build();

var logger = app.Services.GetRequiredService<ILogger<Program>>();
int i = 0;

DateTime begin = default!;
app.MapGet("/", () =>
{
    var c = Interlocked.Increment(ref i);
    if (c == 1) begin = DateTime.UtcNow;

    var elapsed = DateTime.UtcNow - begin;

    logger.ZLogInformation("Count:{0} Elapsed:{1} Rps:{2}", c, elapsed.TotalMilliseconds.ToString("0.00"), (c / elapsed.TotalSeconds).ToString("0.00"));
    return "ok";
});

app.Run();