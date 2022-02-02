namespace DFrame.Controller;

public enum ExecutionStatus
{
    Running,
    Succeed,
    Failed,
    Canceled
}

public class SummarizedExecutionResult
{
    public WorkerId WorkerId { get; }
    public int WorkloadCount { get; }
    public ExecutionStatus ExecutionStatus { get; private set; }

    public bool Error { get; private set; }
    public string? ErrorMessage { get; private set; }
    public int Count { get; private set; }
    public int SucceedCount { get; private set; }
    public int ErrorCount { get; set; }
    public TimeSpan TotalElapsed { get; private set; }
    public TimeSpan Latest { get; private set; }
    public TimeSpan Min { get; private set; }
    public TimeSpan Max { get; private set; }
    public TimeSpan Avg => TimeSpan.FromTicks(TotalElapsed.Ticks / SucceedCount);
    public int CurrentRps => (int)(SucceedCount / TotalElapsed.TotalSeconds);

    // NOTE: Require Median, Percentile?

    public SummarizedExecutionResult(WorkerId workerId, int workloadCount)
    {
        this.WorkerId = workerId;
        this.WorkloadCount = workloadCount;
        this.ExecutionStatus = ExecutionStatus.Running;
    }

    public void Add(ExecuteResult result)
    {
        Count++;
        if (result.HasError)
        {
            ErrorCount++;
            ErrorMessage = result.ErrorMessage;
            return;
        }

        SucceedCount++;

        var elapsed = result.Elapsed;

        Latest = elapsed;
        if (elapsed < Min) Min = elapsed;
        if (Max < elapsed) Max = elapsed;

        TotalElapsed = TimeSpan.FromTicks(Avg.Ticks + elapsed.Ticks);
    }

    // on complete.
    public void TrySetStatus(ExecutionStatus status)
    {
        if (this.ExecutionStatus == ExecutionStatus.Running)
        {
            this.ExecutionStatus = status;
        }
    }
}
