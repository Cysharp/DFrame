using Microsoft.AspNetCore.Mvc;
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

app.MapGet("/reset", () =>
{
    i = 0;
});

app.MapPost("/post_json", ([FromBody] Person p) =>
{
    logger.LogInformation($"Post Json:{p.Age} {p.Name}");
});

app.MapPost("/post_form", (HttpContext ctx) =>
{
    var age = int.Parse(ctx.Request.Form["Age"]);
    var name = ctx.Request.Form["Name"].ToString();

    logger.LogInformation($"Post Form:{age} {name}");
});

app.MapPut("/put_json", ([FromBody] Person p) =>
{
    logger.LogInformation($"Post Json:{p.Age} {p.Name}");
});

app.MapPut("/put_form", (HttpContext ctx) =>
{
    var age = int.Parse(ctx.Request.Form["Age"]);
    var name = ctx.Request.Form["Name"].ToString();

    logger.LogInformation($"Post Form:{age} {name}");
});

app.Run();

public class Person
{
    public int Age { get; set; }
    public string Name { get; set; } = default!;
}