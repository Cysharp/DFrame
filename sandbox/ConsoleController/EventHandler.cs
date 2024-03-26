using DFrame.Controller;
using DFrame.Controller.EventMessage;
using MessagePipe;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace ConsoleController;

/// <summary>
/// Controller event handler
/// </summary>
internal record class EventHandler(ILogger<EventHandler> logger, ISubscriber<ControllerEventMessage> subscriber) : IHostedService
{
    private IDisposable? eventMessageSubscription = null;

    public Task StartAsync(CancellationToken cancellationToken)
    {
        var bag = DisposableBag.CreateBuilder(initialCapacity: 1);
        subscriber.Subscribe(
            eventMessage =>
            {
                switch (eventMessage.MessageType)
                {
                    case ControllerEventMessageType.WorkflowStarted: OnWorkflowStarted(eventMessage.ExecutionSummary); break;
                    case ControllerEventMessageType.SetupCompleted: OnSetupCompleted(eventMessage.ExecutionSummary); break;
                    case ControllerEventMessageType.ExecuteCompleted: OnExecuteCompleted(eventMessage.ExecutionSummary); break;
                    case ControllerEventMessageType.TeardownCompleted: OnTeardownCompleted(eventMessage.ExecutionSummary); break;
                    case ControllerEventMessageType.WorkflowCompleted: OnWorkflowCompleted(eventMessage.ExecutionSummary); break;
                    default: logger.LogWarning("Unknown message type: {0}", eventMessage.MessageType); break;
                }
            }
        ).AddTo(bag);

        eventMessageSubscription = bag.Build();

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        Interlocked.Exchange(ref eventMessageSubscription, null)?.Dispose();
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    void OnWorkflowStarted(ExecutionSummary executionSummary)
        => logger.LogInformation("{0} : {1} : {2}", nameof(ControllerEventMessageType.WorkflowStarted), executionSummary.Workload, executionSummary.ExecutionId);

    /// <inheritdoc/>
    void OnSetupCompleted(ExecutionSummary executionSummary)
        => logger.LogInformation("{0} : {1} : {2}", nameof(ControllerEventMessageType.SetupCompleted), executionSummary.Workload, executionSummary.ExecutionId);

    /// <inheritdoc/>
    void OnExecuteCompleted(ExecutionSummary executionSummary)
        => logger.LogInformation("{0} : {1} : {2}", nameof(ControllerEventMessageType.ExecuteCompleted), executionSummary.Workload, executionSummary.ExecutionId);

    /// <inheritdoc/>
    void OnTeardownCompleted(ExecutionSummary executionSummary)
        => logger.LogInformation("{0} : {1} : {2}", nameof(ControllerEventMessageType.TeardownCompleted), executionSummary.Workload, executionSummary.ExecutionId);

    /// <inheritdoc/>
    void OnWorkflowCompleted(ExecutionSummary executionSummary)
        => logger.LogInformation("{0} : {1} : {2}", nameof(ControllerEventMessageType.WorkflowCompleted), executionSummary.Workload, executionSummary.ExecutionId);

}
