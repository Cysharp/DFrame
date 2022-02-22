using MessagePipe;
using Microsoft.AspNetCore.Components;
using ObservableCollections;
using DFrame.Controller;
using System.Diagnostics.CodeAnalysis;
using DFrame.Pages.Components;
using DFrame.Internal;

namespace DFrame.Pages;

public partial class Index : IDisposable
{
    [Inject] DFrameControllerExecutionEngine engine { get; set; } = default!;
    [Inject] DFrameControllerLogBuffer logRouter { get; set; } = default!;
    [Inject] ILogger<Index> logger { get; set; } = default!;
    [Inject] IExecutionResultHistoryProvider historyProvider { get; set; } = default!;
    [Inject] IScopedPublisher<DrawerRequest> drawerProvider { get; set; } = default!;
    [Inject] LocalStorageAccessor localStorageAccessor { get; set; } = default!;

    ISynchronizedView<string, string> logView = default!;
    IndexViewModel vm = default!;

    // repeat management.
    RepeatModeState? repeatModeState;

    // duration management
    CancellationTokenSource? durationCancellationTokenSource;
    CancellationTokenRegistration? durationCancellationRegistration;

    protected override void OnInitialized()
    {
        logView = logRouter.GetView();
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            var (result, settings) = await localStorageAccessor.TryGetItemAsync<ExecuteSettings>("executeSettings", CancellationToken.None);
            vm = new IndexViewModel(engine, historyProvider, drawerProvider, logView, result ? settings : null);
            await vm.UpdateWorkloadParametersAsync(localStorageAccessor);
            engine.StateChanged += Engine_StateChanged;
            StateHasChanged();
        }
    }

    public void Dispose()
    {
        engine.StateChanged -= Engine_StateChanged;
        vm?.Dispose();
        logView?.Dispose();
    }

    async void Engine_StateChanged()
    {
        await InvokeAsync(() =>
        {
            vm.RefreshEngineProperties(engine);
            StateHasChanged();
        });
    }

    async Task HandleExecute()
    {
        if (vm.SelectedWorkload == null)
        {
            logger.LogInformation("SelectedWorkload is null, does not run workflow.");
            return;
        }
        if (vm.IsRunning)
        {
            logger.LogInformation("Already running, does not run workflow.");
            return;
        }

        var parameters = vm.SelectedWorkloadParameters.Select(x => new KeyValuePair<string, string?>(x.ParameterName, x.Value)).ToArray();

        var totalRequest = (vm.CommandMode == CommandMode.InfiniteLoop || vm.CommandMode == CommandMode.Duration) ? long.MaxValue : vm.TotalRequest;

        var okToStart = engine.StartWorkerFlow(vm.SelectedWorkload, vm.Concurrency, totalRequest, vm.RequestWorkerLimit, parameters!);
        if (!okToStart)
        {
            logger.LogInformation("Invalid parameters, does not run workflow.");
            return;
        }

        if (vm.CommandMode == CommandMode.Repeat)
        {
            repeatModeState = new RepeatModeState(vm.SelectedWorkload, vm.Concurrency, vm.TotalRequest, vm.IncreaseTotalRequestCount, vm.RequestWorkerLimit, vm.IncreaseWorkerCount, vm.RepeatCount, parameters!);
            engine.StateChanged += WatchStateChangedForRepeat;
        }
        if (vm.CommandMode == CommandMode.Duration)
        {
            durationCancellationTokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(vm.DurationTimeSeconds));
            durationCancellationRegistration = durationCancellationTokenSource.Token.Register(() =>
             {
                 engine.Cancel();
             });
        }

        // store lateset settings
        var executeSettings = new ExecuteSettings(vm, engine.CurrentConnectingCount == vm.RequestWorkerLimit);
        await localStorageAccessor.SetItemAsync("executeSettings", executeSettings, CancellationToken.None);
        if (parameters.Length > 0)
        {
            await localStorageAccessor.SetItemAsync($"executeSettings.parameters.{vm.SelectedWorkload}", parameters, CancellationToken.None);
        }
    }

    void WatchStateChangedForRepeat()
    {
        if (!engine.IsRunning)
        {
            // try repeat.
            var state = repeatModeState;
            if (state != null)
            {
                if (state.TryMoveNextRepeat())
                {
                    var okToStart = engine.StartWorkerFlow(state.Workload, state.Concurrency, state.TotalRequest, state.WorkerLimit, state.Parameters!);
                    if (okToStart)
                    {
                        return;
                    }
                    logger.LogInformation("Fail to start repeat execution, stop repeat mode.");
                }

                repeatModeState = null;
                engine.StateChanged -= WatchStateChangedForRepeat;
            }
        }
    }

    void HandleCancel()
    {
        if (repeatModeState != null)
        {
            repeatModeState = null;
            engine.StateChanged -= WatchStateChangedForRepeat;
        }

        durationCancellationRegistration?.Dispose();
        durationCancellationTokenSource?.Dispose();

        engine.Cancel();
    }

    async Task HandleChangeWorkload(ChangeEventArgs e)
    {
        vm.SelectedWorkload = e.Value as string;
        await vm.UpdateWorkloadParametersAsync(localStorageAccessor);
    }

    // only for debugging...
    //async Task Reset()
    //{
    //    vm = new IndexViewModel(engine, historyProvider, drawerProvider, logView, new ExecuteSettings
    //    {
    //        CommandMode = vm.CommandMode,
    //        Workload = vm.SelectedWorkload!,
    //        Concurrency = 1,
    //        TotalRequest = 1,
    //        WorkerLimit = vm.CurrentConnections
    //    });

    //    await localStorageAccessor.RemoveItemAsync("executeSettings", CancellationToken.None);
    //}
}

public class RepeatModeState
{
    public string Workload { get; set; }
    public int Concurrency { get; set; }
    public int RestRepeatCount { get; private set; }
    public long TotalRequest { get; private set; }
    public int WorkerLimit { get; private set; }
    public int IncreaseWorkerLimit { get; }
    public int IncreaseTotalRequest { get; }
    public KeyValuePair<string, string>[] Parameters { get; }

    public RepeatModeState(string workload, int concurrency, long totalRequest, int increaseTotalRequest, int workerLimit, int increaseWorkerLimit, int repeatCount, KeyValuePair<string, string>[] parameters)
    {
        Workload = workload;
        Concurrency = concurrency;
        TotalRequest = totalRequest;
        IncreaseTotalRequest = increaseTotalRequest;
        WorkerLimit = workerLimit;
        IncreaseWorkerLimit = increaseWorkerLimit;
        RestRepeatCount = repeatCount;
        Parameters = parameters;
    }

    public bool TryMoveNextRepeat()
    {
        RestRepeatCount--;
        if (RestRepeatCount <= 0) return false;

        WorkerLimit += IncreaseWorkerLimit;
        TotalRequest += IncreaseTotalRequest;
        return true;
    }
}

public enum CommandMode
{
    Request,
    Repeat,
    Duration,
    InfiniteLoop
}

public class IndexViewModel : IDisposable
{
    readonly IExecutionResultHistoryProvider historyProvider;
    readonly IScopedPublisher<DrawerRequest> drawerProvider;
    readonly ISynchronizedView<string, string> logView;

    // From Engine
    public int CurrentConnections { get; private set; }
    public bool IsRunning { get; private set; }
    public WorkloadInfo[] WorkloadInfos { get; private set; }
    public ExecutionSummary? ExecutionSummary { get; set; }
    public SummarizedExecutionResult[] ExecutionResults { get; private set; }

    // Tab
    public CommandMode CommandMode { get; set; }

    // Input Forms for All
    public string? SelectedWorkload { get; set; }
    public WorkloadParameterInfoViewModel[] SelectedWorkloadParameters { get; private set; }
    public int Concurrency { get; set; } = 1;

    // Request/Repeat
    public long TotalRequest { get; set; } = 1;
    public int RequestWorkerLimit { get; set; }

    // Repeat
    public int IncreaseTotalRequestCount { get; set; } = 0;
    public int IncreaseWorkerCount { get; set; } = 0;
    public int RepeatCount { get; set; } = 1;

    // Duration
    public int DurationTimeSeconds { get; set; } = 1;

    // History
    public int ResultHistoryCount { get; set; }

    public IndexViewModel(DFrameControllerExecutionEngine engine, IExecutionResultHistoryProvider historyProvider, IScopedPublisher<DrawerRequest> drawerProvider, ISynchronizedView<string, string> logView, ExecuteSettings? executeSettings = null)
    {
        this.historyProvider = historyProvider;
        this.drawerProvider = drawerProvider;
        this.logView = logView;

        SelectedWorkloadParameters = Array.Empty<WorkloadParameterInfoViewModel>();
        ResultHistoryCount = historyProvider.GetCount();
        historyProvider.NotifyCountChanged += HistoryProvider_NotifyCountChanged;

        RefreshEngineProperties(engine);

        if (executeSettings != null)
        {
            CommandMode = executeSettings.CommandMode;
            if (executeSettings.Workload != null)
            {
                SelectedWorkload = executeSettings.Workload;
            }
            Concurrency = executeSettings.Concurrency;
            TotalRequest = executeSettings.TotalRequest;
            if (executeSettings.WorkerLimit != null)
            {
                RequestWorkerLimit = executeSettings.WorkerLimit.Value;
            }
            IncreaseTotalRequestCount = executeSettings.IncreaseTotalRequestCount;
            IncreaseWorkerCount = executeSettings.IncreaseWorkerCount;
            RepeatCount = executeSettings.RepeatCount;
            DurationTimeSeconds = executeSettings.DurationTimeSeconds;
        }
    }

    private void HistoryProvider_NotifyCountChanged()
    {
        ResultHistoryCount = historyProvider.GetCount();
    }

    public void Dispose()
    {
        historyProvider.NotifyCountChanged -= HistoryProvider_NotifyCountChanged;
    }

    [MemberNotNull(nameof(WorkloadInfos), nameof(ExecutionResults), nameof(WorkloadInfos))]
    public void RefreshEngineProperties(DFrameControllerExecutionEngine engine)
    {
        if (CurrentConnections == RequestWorkerLimit)
        {
            // auto extend
            this.RequestWorkerLimit = engine.CurrentConnectingCount;
        }
        this.CurrentConnections = engine.CurrentConnectingCount;
        if (CurrentConnections < this.RequestWorkerLimit)
        {
            this.RequestWorkerLimit = CurrentConnections;
        }

        this.IsRunning = engine.IsRunning;
        this.WorkloadInfos = engine.WorkloadInfos;
        if (this.SelectedWorkload == null)
        {
            this.SelectedWorkload = WorkloadInfos.FirstOrDefault()?.Name;
        }
        this.ExecutionSummary = engine.LatestExecutionSummary;
        this.ExecutionResults = engine.LatestSortedSummarizedExecutionResults;
    }

    internal async ValueTask UpdateWorkloadParametersAsync(LocalStorageAccessor localStorageAccessor)
    {
        if (SelectedWorkload != null)
        {
            var info = WorkloadInfos.FirstOrDefault(x => x.Name == SelectedWorkload);
            if (info != null)
            {
                SelectedWorkloadParameters = info.Arguments.Select(x => new IndexViewModel.WorkloadParameterInfoViewModel(x)).ToArray();
            }

            if (SelectedWorkloadParameters.Length != 0)
            {
                var (hasValue, parameters) = await localStorageAccessor.TryGetItemAsync<(string, string?)[]>($"executeSettings.parameters.{SelectedWorkload}", CancellationToken.None);
                if (hasValue)
                {
                    foreach (var parameter in parameters)
                    {
                        var p = SelectedWorkloadParameters.FirstOrDefault(x => x.ParameterName == parameter.Item1);
                        if (p != null)
                        {
                            p.Value = parameter.Item2;
                        }
                    }
                }
            }
        }
    }

    public void ShowServerLogs()
    {
        drawerProvider.Publish(new DrawerRequest
        (
            Title: "Server Log",
            IsShow: true,
            Parameters: null,
            ErrorMessage: null,
            LogView: logView,
            Results: null
        ));
    }

    public void ChangeWorkerLimitRange(ChangeEventArgs e)
    {
        RequestWorkerLimit = int.Parse((string)e.Value!);
    }

    public string TabActive(CommandMode mode)
    {
        return (CommandMode == mode) ? "tab-active" : "";
    }

    public void ChangeCommandMode(CommandMode mode)
    {
        CommandMode = mode;
    }

    public class WorkloadParameterInfoViewModel
    {
        static readonly string[] BoolSelectableValues = new[] { true.ToString(), false.ToString() };
        static readonly string[] NullableBoolSelectableValues = new[] { true.ToString(), false.ToString(), "null" };

        // Modifiable Form
        public string? Value { get; set; }

        public string TypeLabel { get; }
        public string ParameterName { get; }

        public string[] SelectableValues { get; }
        public string Hint { get; }

        public WorkloadParameterInfoViewModel(WorkloadParameterInfo parameterInfo)
        {
            TypeLabel = parameterInfo.GetTypeLabel();
            ParameterName = parameterInfo.ParameterName;

            SelectableValues = Array.Empty<string>();
            if (parameterInfo.ParameterType == AllowParameterType.Boolean)
            {
                if (parameterInfo.IsNullable)
                {
                    Value = "null";
                    SelectableValues = NullableBoolSelectableValues;
                }
                else
                {
                    Value = false.ToString();
                    SelectableValues = BoolSelectableValues;
                }
            }

            if (parameterInfo.ParameterType == AllowParameterType.Enum)
            {
                if (parameterInfo.IsNullable)
                {
                    Value = "null";
                    SelectableValues = parameterInfo.EnumNames.Prepend("null").ToArray();
                }
                else
                {
                    Value = parameterInfo.EnumNames.FirstOrDefault();
                    SelectableValues = parameterInfo.EnumNames;
                }
            }

            if (parameterInfo.DefaultValue != null)
            {
                Value = parameterInfo.DefaultValue?.ToString();
            }

            if (parameterInfo.IsArray)
            {
                Hint = "array value is separated with ','";
            }
            else
            {
                Hint = "";
            }
        }
    }
}

// store latest settings to localstorage
public class ExecuteSettings
{
    public CommandMode CommandMode { get; set; } = CommandMode.Request;
    public string? Workload { get; set; }
    public int Concurrency { get; set; } = 1;
    public int? WorkerLimit { get; set; }  // null as no limit
    public long TotalRequest { get; set; } = 1;

    public int IncreaseTotalRequestCount { get; set; } = 0;
    public int IncreaseWorkerCount { get; set; } = 0;
    public int RepeatCount { get; set; } = 1;
    public int DurationTimeSeconds { get; set; } = 1;

    public ExecuteSettings()
    {

    }

    public ExecuteSettings(IndexViewModel vm, bool isNoLimit)
    {
        this.CommandMode = vm.CommandMode;
        this.Workload = vm.SelectedWorkload;
        this.Concurrency = vm.Concurrency;
        if (!isNoLimit)
        {
            this.WorkerLimit = vm.RequestWorkerLimit;
        }

        switch (vm.CommandMode)
        {
            case CommandMode.Request:
                this.TotalRequest = vm.TotalRequest;
                break;
            case CommandMode.Repeat:
                this.TotalRequest = vm.TotalRequest;
                this.IncreaseTotalRequestCount = vm.IncreaseTotalRequestCount;
                this.IncreaseWorkerCount = vm.IncreaseWorkerCount;
                this.RepeatCount = vm.RepeatCount;
                break;
            case CommandMode.Duration:
                this.DurationTimeSeconds = vm.DurationTimeSeconds;
                break;
            case CommandMode.InfiniteLoop:
            default:
                break;
        }
    }
}