// This file is share with DFrame.Controller and DFrame.
// Original exists in DFrame.Controller.

using MagicOnion;
using MessagePack;
using UnitGenerator;

namespace DFrame;

public interface IControllerHub : IStreamingHub<IControllerHub, IWorkerReceiver>
{
    Task CreateWorkloadCompleteAsync(ExecutionId executionId);
    Task ReportProgressAsync(ExecuteResult result);
    Task ExecuteCompleteAsync();
    Task TeardownCompleteAsync();
}

public interface IWorkerReceiver
{
    void CreateWorkloadAndSetup(ExecutionId executionId, int createCount, string workloadName);
    void Execute(int executeCount);
    void ExecuteUntilReceiveStop();
    void Stop();
    void Teardown();
}

internal static class GenerateOptions
{
    internal const UnitGenerateOptions Guid = UnitGenerateOptions.MessagePackFormatter | UnitGenerateOptions.ParseMethod | UnitGenerateOptions.Comparable | UnitGenerateOptions.WithoutComparisonOperator;
}

[UnitOf(typeof(Guid), GenerateOptions.Guid)]
public readonly partial struct ExecutionId { }

[UnitOf(typeof(Guid), GenerateOptions.Guid)]
public readonly partial struct WorkerId { }

[UnitOf(typeof(Guid), GenerateOptions.Guid)]
public readonly partial struct WorkloadId { }

[MessagePackObject]
public class ExecuteResult
{
    [Key(0)]
    public WorkloadId WorkloadId { get; }
    [Key(1)]
    public TimeSpan Elapsed { get; }
    [Key(2)]
    public int ExecutionNo { get; }
    [Key(3)]
    public bool HasError { get; }
    [Key(4)]
    public string? ErrorMessage { get; }

    public ExecuteResult(WorkloadId workloadId, TimeSpan elapsed, int executionNo, bool hasError, string? errorMessage)
    {
        WorkloadId = workloadId;
        Elapsed = elapsed;
        ExecutionNo = executionNo;
        HasError = hasError;
        ErrorMessage = errorMessage;
    }
}