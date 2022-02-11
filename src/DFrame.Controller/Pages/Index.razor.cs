using MessagePipe;
using Microsoft.AspNetCore.Components;
using ObservableCollections;
using DFrame.Controller;
using System.Diagnostics.CodeAnalysis;

namespace DFrame.Pages;

public partial class Index : IDisposable
{
    // TODO:rename this name.
    [Inject] DFrameControllerExecutionEngine engine { get; set; } = default!;
    [Inject] LogRouter logRouter { get; set; } = default!;
    [Inject] ILogger<Index> logger { get; set; } = default!;

    // TODO:to vm?
    ISynchronizedView<string, string> logView = default!;

    IndexViewModel vm = default!;


    protected override void OnInitialized()
    {
        vm = new IndexViewModel(engine);

        engine.StateChanged += Engine_StateChanged;

        logView = logRouter.GetView();
        logView.CollectionStateChanged += LogView_CollectionStateChanged;
    }

    public void Dispose()
    {
        engine.StateChanged -= Engine_StateChanged;
        logView.CollectionStateChanged -= LogView_CollectionStateChanged;
    }

    async void Engine_StateChanged()
    {
        await InvokeAsync(() =>
        {
            vm.RefreshEngineProperties(engine);
            StateHasChanged();
        });
    }

    private async void LogView_CollectionStateChanged(System.Collections.Specialized.NotifyCollectionChangedAction obj)
    {
        await InvokeAsync(StateHasChanged);
    }

    void HandleExecute()
    {
        if (vm.SelectedWorkload == null)
        {
            logger.LogDebug("SelectedWorkload is null, does not run workflow.");
            return;
        }
        if (vm.IsRunning)
        {
            logger.LogDebug("Already running, does not run workflow.");
            return;
        }

        var parameters = vm.SelectedWorkloadParametes.Select(x => (x.ParameterName, x.Value)).ToArray();
        engine.StartWorkerFlow(vm.SelectedWorkload, vm.Concurrency, vm.TotalRequest, parameters!);
    }

    // TODO:cancel
    void HandleCancel()
    {
    }
}

public enum CommandMode
{
    Request,
    Repeat,
    InfiniteLoop
}

public class IndexViewModel
{
    // From Engine
    public int CurrentConnections { get; private set; }
    public bool IsRunning { get; private set; }
    public WorkloadInfo[] WorkloadInfos { get; private set; }
    public SummarizedExecutionResult[] ExecutionResults { get; private set; }

    // Tab
    public CommandMode CommandMode { get; set; }

    // Input Forms
    public string? SelectedWorkload { get; set; }
    public WorkloadParameterInfoViewModel[] SelectedWorkloadParametes { get; private set; }

    public int Concurrency { get; set; } = 1;
    public int TotalRequest { get; set; } = 1;
    public int? RequestWorkerCount { get; set; } // null is all

    public IndexViewModel(DFrameControllerExecutionEngine engine)
    {
        SelectedWorkloadParametes = Array.Empty<WorkloadParameterInfoViewModel>();
        RefreshEngineProperties(engine);
    }

    [MemberNotNull(nameof(WorkloadInfos), nameof(ExecutionResults), nameof(WorkloadInfos))]
    public void RefreshEngineProperties(DFrameControllerExecutionEngine engine)
    {
        this.CurrentConnections = engine.CurrentConnectingCount;
        this.IsRunning = engine.IsRunning;
        this.WorkloadInfos = engine.WorkloadInfos;
        if (this.SelectedWorkload == null)
        {
            this.SelectedWorkload = WorkloadInfos.FirstOrDefault()?.Name;
        }
        this.ExecutionResults = engine.LatestSortedSummarizedExecutionResults;
    }

    // TODO:log view.

    // TODO:remove this
    //public IReadOnlyDictionary<string, string> GetMetadataOfWorker(WorkerId id)
    //{
    //    return engine.GetMetadata(id);
    //}

    public void ChangeSelectedWorkload(ChangeEventArgs e)
    {
        this.SelectedWorkload = e.Value as string;
        if (this.SelectedWorkload != null)
        {
            var p = WorkloadInfos.FirstOrDefault(x => x.Name == this.SelectedWorkload);
            if (p != null)
            {
                this.SelectedWorkloadParametes = p.Arguments.Select(x => new WorkloadParameterInfoViewModel(x)).ToArray();
            }
        }
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