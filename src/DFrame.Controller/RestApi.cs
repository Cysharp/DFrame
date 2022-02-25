using DFrame.Controller;
using Microsoft.AspNetCore.Mvc;

namespace DFrame
{
    public static class RestApi
    {
        public static void RegisterRestApi(WebApplication app)
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

            app.MapGet("api/resultscount", (IExecutionResultHistoryProvider provider) =>
            {
                return provider.GetCount();
            });

            app.MapGet("api/resultslist", (IExecutionResultHistoryProvider provider) =>
            {
                return provider.GetList();
            });

            app.MapGet("api/getresult", (IExecutionResultHistoryProvider provider, ExecutionId executionId) =>
            {
                var r = provider.GetResult(executionId);
                if (r == null) return null;
                return new { summary = r.Value.Summary, results = r.Value.Results };
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
}
