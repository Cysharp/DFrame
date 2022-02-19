using DFrame.Controller;
using MessagePipe;
using Microsoft.AspNetCore.Components;

namespace DFrame.Pages.Components;

public partial class ResultFooter
{
    [Parameter, EditorRequired]
    public bool IsRunning { get; set; }

    [Parameter, EditorRequired]
    public ExecutionSummary? ExecutionSummary { get; set; }

    [Parameter, EditorRequired]
    public IReadOnlyList<SummarizedExecutionResult> ExecutionResults { get; set; } = Array.Empty<SummarizedExecutionResult>();
}