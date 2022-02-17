using DFrame.Controller;
using Microsoft.AspNetCore.Components;

namespace DFrame.Pages;

public partial class ExecutionResultTable
{
    [Parameter, EditorRequired]
    public bool IsRunning { get; set; }

    [Parameter, EditorRequired]
    public ExecutionSummary? ExecutionSummary { get; set; }

    [Parameter, EditorRequired]
    public SummarizedExecutionResult[] ExecutionResults { get; set; } = default!;
}