using DFrame.Controller;
using MessagePipe;
using Microsoft.AspNetCore.Components;

namespace DFrame.Pages;

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
            IsShow: true,
            Parameters: ExecutionSummary?.Parameters,
            ErrorMessage: null,
            LogView: null
        ));
    }
}