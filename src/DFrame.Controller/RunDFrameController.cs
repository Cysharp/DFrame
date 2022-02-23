using DFrame.Controller;
using DFrame.Internal;
using MagicOnion.Server;
using MessagePack;
using MessagePipe;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace DFrame;

public static class DFrameControllerWebApplicationBuilderExtensions
{
    public static Task RunDFrameControllerAsync(this WebApplicationBuilder appBuilder)
    {
        return RunDFrameControllerAsync(appBuilder, new DFrameControllerOptions(), (_, __) => { });
    }

    public static Task RunDFrameControllerAsync(this WebApplicationBuilder appBuilder, DFrameControllerOptions options)
    {
        return RunDFrameControllerAsync(appBuilder, options, (_, __) => { });
    }

    public static Task RunDFrameControllerAsync(this WebApplicationBuilder appBuilder, Action<DFrameControllerOptions> configureOptions)
    {
        return RunDFrameControllerAsync(appBuilder, new DFrameControllerOptions(), (_, x) => configureOptions(x));
    }

    public static Task RunDFrameControllerAsync(this WebApplicationBuilder appBuilder, Action<WebHostBuilderContext, DFrameControllerOptions> configureOptions)
    {
        return RunDFrameControllerAsync(appBuilder, new DFrameControllerOptions(), configureOptions);
    }

    static async Task RunDFrameControllerAsync(WebApplicationBuilder appBuilder, DFrameControllerOptions options, Action<WebHostBuilderContext, DFrameControllerOptions> configureOptions)
    {
        appBuilder.WebHost.ConfigureServices((WebHostBuilderContext ctx, IServiceCollection services) =>
        {
            services.AddGrpc();
            services.AddMagicOnion(x =>
            {
                // Should use same options between DFrame.Controller(this) and DFrame.Worker
                x.SerializerOptions = MessagePackSerializerOptions.Standard;
            });
            services.AddSingleton<IMagicOnionLogger, MagicOnionLogToLogger>();

            services.AddRazorPages()
                .ConfigureApplicationPartManager(manager =>
                {
                    // import libraries razor pages
                    var assembly = typeof(DFrameControllerWebApplicationBuilderExtensions).Assembly;
                    var assemblyPart = new CompiledRazorAssemblyPart(assembly);
                    manager.ApplicationParts.Add(assemblyPart);
                });

            services.AddServerSideBlazor();

            // DFrame Options
            services.TryAddSingleton<DFrameControllerExecutionEngine>();
            services.TryAddSingleton<DFrameControllerLogBuffer>();
            services.AddSingleton<ILoggerProvider, DFrameControllerLoggerProvider>();
            services.AddScoped<LocalStorageAccessor>();
            configureOptions(ctx, options);
            services.AddSingleton(options);

            // If user sets custom provdier, use it.
            services.TryAddSingleton<IExecutionResultHistoryProvider, InMemoryExecutionResultHistoryProvider>();

            services.AddMessagePipe();
        });

        var app = appBuilder.Build();

        app.UseStaticFiles();
        app.UseRouting();

        app.MapBlazorHub();
        app.MapFallbackToPage("/_Host");

        app.MapMagicOnionService();

        DisplayConfiguration(app);

        if (!options.DisableRestApi)
        {
            RegisterRestApi(app);
        }

        await app.RunAsync();
    }

    static void DisplayConfiguration(WebApplication app)
    {
        var config = app.Services.GetRequiredService<IConfiguration>();

        var http1Endpoint = config.GetSection("Kestrel:Endpoints:Http:Url");
        if (http1Endpoint != null && http1Endpoint.Value != null)
        {
            app.Logger.LogInformation($"Hosting DFrame.Controller on {http1Endpoint.Value}. You can open this address by browser.");
        }

        var gprcEndpoint = config.GetSection("Kestrel:Endpoints:Grpc:Url");
        if (gprcEndpoint != null && gprcEndpoint.Value != null)
        {
            app.Logger.LogInformation($"Hosting MagicOnion(gRPC) address on {gprcEndpoint.Value}. Setup this address to DFrameWorkerOptions.ControllerAddress.");
        }
    }

    static void RegisterRestApi(WebApplication app)
    {
        // field
        Pages.RepeatModeState? repeatModeState = null;
        CancellationTokenSource? durationCancellationTokenSource = null;
        CancellationTokenRegistration? durationCancellationRegistration = default;
        CancellationTokenSource? repeatCancellation = null;

        // mode
        app.MapPost("api/request", (DFrameControllerExecutionEngine engine, [FromBody] RequestBody request) =>
        {
            StartRequest(engine, request.Workload, request.Concurrency, request.TotalRequest, request.Workerlimit, request.Parameters, out var result);
            return result;
        });

        app.MapPost("api/repeat", (DFrameControllerExecutionEngine engine, [FromBody] RepeatBody request) =>
        {
            var workerLimit = request.Workerlimit ?? engine.CurrentConnectingCount;
            var ok = StartRequest(engine, request.Workload, request.Concurrency, request.TotalRequest, workerLimit, request.Parameters, out var result);
            if (!ok) return result;

            repeatModeState = new Pages.RepeatModeState(request.Workload, request.Concurrency, request.TotalRequest,
                request.IncreaseTotalWorker, workerLimit, request.IncreaseTotalWorker, request.RepeatCount, request.Parameters?.ToArray() ?? Array.Empty<KeyValuePair<string, string?>>());
            repeatCancellation = new CancellationTokenSource();

            Action WatchStateChangedForRepeat = null!;
            WatchStateChangedForRepeat = () =>
            {
                if (!engine.IsRunning)
                {
                    // try repeat.
                    var state = repeatModeState;
                    if (state != null)
                    {
                        if (!repeatCancellation.IsCancellationRequested && state.TryMoveNextRepeat())
                        {
                            var okToStart = engine.StartWorkerFlow(state.Workload, state.Concurrency, state.TotalRequest, state.WorkerLimit, state.Parameters!);
                            if (okToStart)
                            {
                                return;
                            }
                        }

                        repeatModeState = null;
                        engine.StateChanged -= WatchStateChangedForRepeat;
                    }
                }
            };
            engine.StateChanged += WatchStateChangedForRepeat;

            return result;
        });

        app.MapPost("api/duration", (DFrameControllerExecutionEngine engine, [FromBody] DurationBody request) =>
        {
            var totalRequest = long.MaxValue;
            var ok = StartRequest(engine, request.Workload, request.Concurrency, totalRequest, request.Workerlimit, request.Parameters, out var result);
            if (!ok) return result;

            durationCancellationTokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(request.ExecuteTimeSeconds));
            durationCancellationRegistration = durationCancellationTokenSource.Token.Register(() =>
            {
                engine.Cancel();
            });

            return result;
        });

        app.MapPost("api/infinite", (DFrameControllerExecutionEngine engine, [FromBody] InfiniteBody request) =>
        {
            var totalRequest = long.MaxValue;
            StartRequest(engine, request.Workload, request.Concurrency, totalRequest, request.Workerlimit, request.Parameters, out var result);
            return result;
        });

        app.MapPost("api/cancel", (DFrameControllerExecutionEngine engine) =>
        {
            if (repeatModeState != null)
            {
                repeatCancellation?.Cancel();
            }

            durationCancellationRegistration?.Dispose();
            durationCancellationTokenSource?.Dispose();
            engine.Cancel();
        });

        app.MapGet("api/isrunning", (DFrameControllerExecutionEngine engine) =>
        {
            return engine.IsRunning;
        });

        app.MapGet("api/connections", (DFrameControllerExecutionEngine engine) =>
        {
            return engine.CurrentConnectingCount;
        });

        app.MapGet("api/latestresult", (DFrameControllerExecutionEngine engine) =>
        {
            return new { summary = engine.LatestExecutionSummary, results = engine.LatestSortedSummarizedExecutionResults };
        });

        static bool StartRequest(DFrameControllerExecutionEngine engine, string workload, int concurrency, long totalRequest, int? workerlimit, Dictionary<string, string?>? parameters, out IResult result)
        {
            if (engine.IsRunning)
            {
                result = Results.BadRequest("worker is already running.");
                return false;
            }

            try
            {
                var ok = engine.StartWorkerFlow(workload, concurrency, totalRequest, workerlimit ?? engine.CurrentConnectingCount, parameters?.ToArray() ?? Array.Empty<KeyValuePair<string, string?>>());
                if (!ok)
                {
                    result = Results.BadRequest("can not start.");
                    return false;
                }
            }
            catch (InvalidOperationException ex)
            {
                result = Results.BadRequest(ex.Message);
                return false;

            }

            result = Results.Ok(engine.LatestExecutionSummary);
            return true;
        }
    }

    class RequestBody
    {
        public string Workload { get; set; } = default!;
        public Dictionary<string, string?>? Parameters { get; set; }
        public int Concurrency { get; set; }
        public int? Workerlimit { get; set; }
        public long TotalRequest { get; set; }
    }

    class RepeatBody
    {
        public string Workload { get; set; } = default!;
        public Dictionary<string, string?>? Parameters { get; set; }
        public int Concurrency { get; set; }
        public int? Workerlimit { get; set; }
        public long TotalRequest { get; set; }
        public int IncreaseTotalRequest { get; set; }
        public int IncreaseTotalWorker { get; set; }
        public int RepeatCount { get; set; }
    }

    class DurationBody
    {
        public string Workload { get; set; } = default!;
        public Dictionary<string, string?>? Parameters { get; set; }
        public int Concurrency { get; set; }
        public int? Workerlimit { get; set; }
        public int ExecuteTimeSeconds { get; set; }
    }

    class InfiniteBody
    {
        public string Workload { get; set; } = default!;
        public Dictionary<string, string?>? Parameters { get; set; }
        public int Concurrency { get; set; }
        public int? Workerlimit { get; set; }
    }
}
