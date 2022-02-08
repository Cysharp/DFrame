using MessagePipe;
using Microsoft.AspNetCore.Components;
using ObservableCollections;
using DFrame.Controller;

namespace DFrame.Pages;

public partial class Index : IDisposable
{
    [Inject]
    public DFrameControllerExecutionEngine ConnectionGroupContext { get; set; } = default!;

    [Inject]
    public LogRouter LogRouter { get; set; } = default!;

    ISynchronizedView<string, string> logView = default!;
    InputFormModel inputFormModel = new InputFormModel();

    int GetCurrentConnectingCount() => ConnectionGroupContext.CurrentConnectingCount;
    SummarizedExecutionResult[] GetRunnningResults() => ConnectionGroupContext.LatestSortedSummarizedExecutionResults;

    protected override void OnInitialized()
    {
        ConnectionGroupContext.StateChanged += ConnectionGroupContext_StateChanged;

        logView = LogRouter.GetView();
        logView.CollectionStateChanged += View_CollectionStateChanged;
    }

    public void Dispose()
    {
        ConnectionGroupContext.StateChanged -= ConnectionGroupContext_StateChanged;
        logView.CollectionStateChanged -= View_CollectionStateChanged;
    }

    async void ConnectionGroupContext_StateChanged()
    {
        await InvokeAsync(StateHasChanged);
    }

    private async void View_CollectionStateChanged(System.Collections.Specialized.NotifyCollectionChangedAction obj)
    {
        await InvokeAsync(StateHasChanged);
    }

    void HandleSubmit()
    {
        if (inputFormModel.WorkloadName == null)
        {
            // Invalid...
            return;
        }

        if (ConnectionGroupContext.IsRunning) // can not invoke
        {
            return;
        }

        ConnectionGroupContext.StartWorkerFlow(inputFormModel.WorkloadName, inputFormModel.WorkloadPerWorker, inputFormModel.ExecutePerWorkload);
    }

    // TODO: rename?
    // concurrency(workload-per-worker)
    // totalrequestcount
    public class InputFormModel
    {
        public string? WorkloadName { get; set; }
        public int WorkloadPerWorker { get; set; } = 1;
        public int ExecutePerWorkload { get; set; } = 1;
    }


    public enum CommandMode
    {
        Request,
        InfiniteLoop
    }

    public class IndexViewModel
    {
        readonly DFrameControllerExecutionEngine connectionGroupContext;

        public IndexViewModel(DFrameControllerExecutionEngine connectionGroupContext)
        {
            this.connectionGroupContext = connectionGroupContext;
        }

        public int CurrentConnections => connectionGroupContext.CurrentConnectingCount;
        public bool IsRunning => connectionGroupContext.IsRunning;
        public WorkloadInfo[] WorkloadInfos => connectionGroupContext.WorkloadInfos;
        public SummarizedExecutionResult[] ExecutionResults => connectionGroupContext.LatestSortedSummarizedExecutionResults;

        // Button change
        public CommandMode CommandMode { get; set; }

        // Form Models...
        public int Concurrency { get; set; }
        public int TotalRequest { get; set; }
        public int? RequestWorkers { get; set; } // null is all

        // from logger
        public RingBuffer<string> Logs { get; set; } = new RingBuffer<string>(100);

        public IReadOnlyDictionary<string, string> GetMetadataOfWorker(WorkerId id)
        {
            return connectionGroupContext.GetMetadata(id);
        }
    }
}