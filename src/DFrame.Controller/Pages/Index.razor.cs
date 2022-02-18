using MessagePipe;
using Microsoft.AspNetCore.Components;
using ObservableCollections;
using DFrame.Controller;
using System.Diagnostics.CodeAnalysis;
using DFrame.Utilities;

namespace DFrame.Pages;

public partial class Index : IDisposable
{
    [Inject] DFrameControllerExecutionEngine engine { get; set; } = default!;
    [Inject] LogRouter logRouter { get; set; } = default!;
    [Inject] ILogger<Index> logger { get; set; } = default!;
    [Inject] IExecutionResultHistoryProvider historyProvider { get; set; } = default!;
    [Inject] IScopedPublisher<DrawerRequest> drawerProvider { get; set; } = default!;
    [Inject] LocalStorageAccessor localStorageAccessor { get; set; } = default!;

    ISynchronizedView<string, string> logView = default!;
    IndexViewModel vm = default!;

    // repeat management.
    RepeatModeState? repeatModeState;

    protected override void OnInitialized()
    {
        logView = logRouter.GetView();
        vm = new IndexViewModel(engine, historyProvider, drawerProvider, logView);
        engine.StateChanged += Engine_StateChanged;
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            var (result, settings) = await localStorageAccessor.TryGetItemAsync<ExecuteSettings>("executeSettings", CancellationToken.None);

            if (result)
            {
                vm = new IndexViewModel(engine, historyProvider, drawerProvider, logView, settings);
                engine.StateChanged += Engine_StateChanged;
                StateHasChanged();
            }
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

        var parameters = vm.SelectedWorkloadParameters.Select(x => (x.ParameterName, x.Value)).ToArray();

        var totalRequest = (vm.CommandMode == CommandMode.InfiniteLoop) ? int.MaxValue : vm.TotalRequest;

        engine.StartWorkerFlow(vm.SelectedWorkload, vm.Concurrency, totalRequest, vm.RequestWorkerLimit, parameters!);

        if (vm.CommandMode == CommandMode.Repeat)
        {
            repeatModeState = new RepeatModeState(vm.SelectedWorkload, vm.Concurrency, vm.TotalRequest, vm.IncreaseTotalRequestCount, vm.RequestWorkerLimit, vm.IncreaseWorkerCount, vm.RepeatCount, parameters!);
            engine.StateChanged += WatchStateChangedForRepeat;
        }

        var executeSettings = new ExecuteSettings
        {
            CommandMode = vm.CommandMode,
            Workload = vm.SelectedWorkload,
            Concurrency = vm.Concurrency,
            TotalRequest = totalRequest,
            WorkerLimit = vm.RequestWorkerLimit,
            IncreaseTotalRequestCount = vm.IncreaseTotalRequestCount,
            IncreaseWorkerCount = vm.IncreaseWorkerCount,
            RepeatCount = vm.RepeatCount,
            Parameters = parameters.ToDictionary(x => x.ParameterName, x => x.Value)
        };

        await localStorageAccessor.SetItemAsync("executeSettings", executeSettings, CancellationToken.None);
    }

    private void WatchStateChangedForRepeat()
    {
        if (!engine.IsRunning)
        {
            // try repeat.
            var state = repeatModeState;
            if (state != null)
            {
                if (state.TryMoveNextRepeat())
                {
                    engine.StartWorkerFlow(state.Workload, state.Concurrency, state.TotalRequest, state.WorkerLimit, state.Parameters!);
                }
                else
                {
                    repeatModeState = null;
                    engine.StateChanged -= WatchStateChangedForRepeat;
                }
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

        engine.Cancel();
    }
}

public class RepeatModeState
{
    public string Workload { get; set; }
    public int Concurrency { get; set; }
    public int RestRepeatCount { get; private set; }
    public int TotalRequest { get; private set; }
    public int WorkerLimit { get; private set; }
    public int IncreaseWorkerLimit { get; }
    public int IncreaseTotalRequest { get; }
    public (string, string)[] Parameters { get; }

    public RepeatModeState(string workload, int concurrency, int totalRequest, int increaseTotalRequest, int workerLimit, int increaseWorkerLimit, int repeatCount, (string, string)[] parameters)
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
    public int TotalRequest { get; set; } = 1;
    public int RequestWorkerLimit { get; set; }

    // Repeat
    public int IncreaseTotalRequestCount { get; set; } = 0;
    public int IncreaseWorkerCount { get; set; } = 0;
    public int RepeatCount { get; set; } = 1;

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
            SelectedWorkload = executeSettings.Workload;
            Concurrency = executeSettings.Concurrency;
            TotalRequest = executeSettings.TotalRequest;
            RequestWorkerLimit = executeSettings.WorkerLimit;
            IncreaseTotalRequestCount = executeSettings.IncreaseTotalRequestCount;
            IncreaseWorkerCount = executeSettings.IncreaseWorkerCount;
            RepeatCount = executeSettings.RepeatCount;

            UpdateWorkload();

            if (executeSettings.Parameters != null)
            {
                foreach (var parameter in executeSettings.Parameters)
                {
                    var p = SelectedWorkloadParameters.FirstOrDefault(x => x.ParameterName == parameter.Key);

                    if (p != null)
                    {
                        p.Value = parameter.Value;
                    }
                }
            }
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

    public void ShowServerLogs()
    {
        drawerProvider.Publish(new DrawerRequest
        (
            IsShow: true,
            Parameters: null,
            ErrorMessage: null,
            LogView: logView
        ));
    }

    public void ChangeSelectedWorkload(ChangeEventArgs e)
    {
        this.SelectedWorkload = e.Value as string;
        UpdateWorkload();
    }

    public void UpdateWorkload()
    {
        if (this.SelectedWorkload != null)
        {
            var p = WorkloadInfos.FirstOrDefault(x => x.Name == this.SelectedWorkload);
            if (p != null)
            {
                this.SelectedWorkloadParameters = p.Arguments.Select(x => new WorkloadParameterInfoViewModel(x)).ToArray();
            }
        }
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

public class ExecuteSettings
{
    public CommandMode CommandMode { get; set; }
    public string Workload { get; set; } = default!;
    public int Concurrency { get; set; }
    public int TotalRequest { get; set; }
    public int WorkerLimit { get; set; }
    public int IncreaseTotalRequestCount { get; set; }
    public int IncreaseWorkerCount { get; set; }
    public int RepeatCount { get; set; }
    public Dictionary<string, string?>? Parameters { get; set; } = default!;
}
