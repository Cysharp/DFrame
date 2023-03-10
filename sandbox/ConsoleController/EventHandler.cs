using DFrame.Controller;
using Microsoft.Extensions.Logging;

namespace ConsoleController;

internal class EventHandler : IEventHandler
{
    readonly ILogger logger;

    public EventHandler(ILogger<EventHandler> logger) => this.logger = logger;

    /// <inheritdoc/>
    void IEventHandler.OnWorkflowStarted(ExecutionSummary executionSummary)
        => logger.LogInformation("{0} : {1} : {2}", nameof(IEventHandler.OnWorkflowStarted), executionSummary.Workload, executionSummary.ExecutionId);

    /// <inheritdoc/>
    void IEventHandler.OnSetupCompleted(ExecutionSummary executionSummary)
        => logger.LogInformation("{0} : {1} : {2}", nameof(IEventHandler.OnSetupCompleted), executionSummary.Workload, executionSummary.ExecutionId);

    /// <inheritdoc/>
    void IEventHandler.OnExecuteCompleted(ExecutionSummary executionSummary)
        => logger.LogInformation("{0} : {1} : {2}", nameof(IEventHandler.OnExecuteCompleted), executionSummary.Workload, executionSummary.ExecutionId);

    /// <inheritdoc/>
    void IEventHandler.OnTeardownCompleted(ExecutionSummary executionSummary)
        => logger.LogInformation("{0} : {1} : {2}", nameof(IEventHandler.OnTeardownCompleted), executionSummary.Workload, executionSummary.ExecutionId);

    /// <inheritdoc/>
    void IEventHandler.OnWorkflowCompleted(ExecutionSummary executionSummary)
        => logger.LogInformation("{0} : {1} : {2}", nameof(IEventHandler.OnWorkflowCompleted), executionSummary.Workload, executionSummary.ExecutionId);
}
