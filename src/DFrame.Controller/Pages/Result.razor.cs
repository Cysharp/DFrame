using DFrame.Controller;
using Microsoft.AspNetCore.Components;

namespace DFrame.Pages;

public partial class Result
{
    [Inject] IExecutionResultHistoryProvider historyProvider { get; set; } = default!;

    [Parameter] public Guid executionId { get; set; }

    ExecutionSummary summary = default!;
    IReadOnlyList<SummarizedExecutionResult> results = Array.Empty<SummarizedExecutionResult>();

    protected override void OnInitialized()
    {
        var r = historyProvider.GetResult(new ExecutionId(executionId));
        if (r == null) return;

        (summary, results) = r.Value;
    }
}