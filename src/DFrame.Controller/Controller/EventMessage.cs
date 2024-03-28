namespace DFrame.Controller.EventMessage;

/// <summary>
/// Type of <see cref="ControllerEventMessage"/>
/// </summary>
public enum ControllerEventMessageType
{
    WorkflowStarted,
    SetupCompleted,
    ExecuteCompleted,
    TeardownCompleted,
    WorkflowCompleted,
}

/// <summary>
/// Controller event message.
/// </summary>
public record class ControllerEventMessage(ControllerEventMessageType MessageType, ExecutionSummary ExecutionSummary);

