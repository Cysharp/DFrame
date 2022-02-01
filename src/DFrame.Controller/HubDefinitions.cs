using MagicOnion;
using MessagePack;

namespace DFrame;

public interface IControllerHub : IStreamingHub<IControllerHub, IWorkerReceiver>
{
    Task CreateWorkloadCompleteAsync(Guid executionId);
    Task ReportProgressAsync(ExecuteResult result);
    Task ExecuteCompleteAsync(ExecuteResult[] result);
    Task TeardownCompleteAsync();
}

public interface IWorkerReceiver
{
    void CreateWorkloadAndSetup(Guid executionId, int createCount, string workloadName);
    void Execute(int executeCount);
    void ExecuteUntilReceiveStop();
    void Stop();
    void Teardown();
}

[MessagePackObject]
public class ExecuteResult
{
    [Key(0)]
    public string WorkloadId { get; }
    [Key(1)]
    public TimeSpan Elapsed { get; }
    [Key(2)]
    public int ExecutionNo { get; }
    [Key(3)]
    public bool HasError { get; }
    [Key(4)]
    public string? ErrorMessage { get; }

    public ExecuteResult(string workerId, TimeSpan elapsed, int executionNo, bool hasError, string? errorMessage)
    {
        WorkloadId = workerId;
        Elapsed = elapsed;
        ExecutionNo = executionNo;
        HasError = hasError;
        ErrorMessage = errorMessage;
    }
}