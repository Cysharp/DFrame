namespace DFrame.Controller;

public interface IEventHandler
{
    /// <summary>
    /// <see cref="DFrameControllerExecutionEngine.StartWorkerFlow(string, int, long, int, KeyValuePair{string, string?}[])"/>
    /// </summary>
    void OnWorkflowStarted(ExecutionSummary executionSummary) { }

    /// <summary>
    /// <see cref="DFrameControllerExecutionEngine.CreateWorkloadAndSetupComplete(WorkerId, IWorkerReceiver, IWorkerReceiver)"/>
    /// </summary>
    void OnSetupCompleted(ExecutionSummary executionSummary) { }

    /// <summary>
    /// <see cref="DFrameControllerExecutionEngine.ExecuteComplete(WorkerId, Dictionary{WorkloadId, Dictionary{string, string}?})"/>
    /// </summary>
    void OnExecuteCompleted(ExecutionSummary executionSummary) { }

    /// <summary>
    /// <see cref="DFrameControllerExecutionEngine.TeardownComplete(WorkerId)"/>
    /// </summary>
    void OnTeardownCompleted(ExecutionSummary executionSummary) { }

    /// <summary>
    /// <see cref="DFrameControllerExecutionEngine.TeardownComplete(WorkerId)"/>
    /// </summary>
    void OnWorkflowCompleted(ExecutionSummary executionSummary) { }
}

