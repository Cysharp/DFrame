using DFrame.Controller;
using MessagePipe;
using Microsoft.AspNetCore.Components;

namespace DFrame.Pages.Components;

public partial class ExecutionResultTable
{
    [Inject] IScopedPublisher<DrawerRequest> drawerPublisher { get; set; } = default!;

    [Parameter, EditorRequired]
    public bool IsRunning { get; set; }

    [Parameter]
    public bool ShowHeader { get; set; }

    [Parameter, EditorRequired]
    public ExecutionSummary? ExecutionSummary { get; set; }

    [Parameter, EditorRequired]
    public IReadOnlyList<SummarizedExecutionResult> ExecutionResults { get; set; } = Array.Empty<SummarizedExecutionResult>();

    void ShowParameters()
    {
        drawerPublisher.Publish(new DrawerRequest
        (
            Kind: "Parameters",
            Title: ExecutionSummary?.Workload,
            IsShow: true,
            Parameters: ExecutionSummary?.Parameters,
            ErrorMessage: null,
            LogView: null,
            Results: null
        )); ;
    }

    void ShowWorkerInfo(WorkerId workerId)
    {
        var result = ExecutionResults.FirstOrDefault(x => x.WorkerId == workerId);

        drawerPublisher.Publish(new DrawerRequest
        (
            Kind: "Worker",
            Title: (result == null) ? "" : result.WorkerId.ToString(),
            IsShow: true,
            Parameters: result?.Metadata!,
            ErrorMessage: result?.ErrorMessage,
            LogView: null,
            Results: result?.Results
        ));
    }
}