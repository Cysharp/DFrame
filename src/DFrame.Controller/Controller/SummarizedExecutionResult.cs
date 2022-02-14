namespace DFrame.Controller;

public enum ExecutionStatus
{
    Running,
    Succeed,
    Failed,
    // Canceled
}

public class SummarizedExecutionResult
{
    DateTime? executeBegin;
    DateTime? executeCompleted;
    TimeSpan elapsedSum;

    public WorkerId WorkerId { get; }
    internal Guid ConnectionId { get; }
    public int WorkloadCount { get; }
    public ExecutionStatus ExecutionStatus { get; private set; }

    public bool Error { get; private set; }
    public string? ErrorMessage { get; private set; }
    public int Count { get; private set; }
    public int SucceedCount { get; private set; }
    public int ErrorCount { get; set; }
    public TimeSpan TotalElapsed => elapsedSum;
    public TimeSpan Latest { get; private set; }
    public TimeSpan Min { get; private set; }
    public TimeSpan Max { get; private set; }
    public TimeSpan Avg => (SucceedCount == 0) ? TimeSpan.Zero : TimeSpan.FromTicks(elapsedSum.Ticks / SucceedCount);
    public double CurrentRps => (TotalElapsed.TotalSeconds == 0 || (executeBegin == null)) ? 0 : (SucceedCount / RunningTime.TotalSeconds);

    public TimeSpan RunningTime
    {
        get
        {
            if (executeBegin == null)
            {
                return TimeSpan.Zero;
            }

            if (executeCompleted == null)
            {
                return DateTime.UtcNow - executeBegin.Value;
            }

            return executeCompleted.Value - executeBegin.Value;
        }
    }

    // NOTE: Require Median, Percentile?

    public SummarizedExecutionResult(WorkerId workerId, Guid connectionId, int workloadCount)
    {
        this.WorkerId = workerId;
        this.ConnectionId = connectionId;
        this.WorkloadCount = workloadCount;
        this.ExecutionStatus = ExecutionStatus.Running;
    }

    public void InitExecuteBeginTime(DateTime executeBegin)
    {
        if (this.executeBegin == null)
        {
            this.executeBegin = executeBegin;
        }
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
        if (SucceedCount == 1)
        {
            Min = Max = elapsed;
        }
        else
        {
            if (elapsed < Min) Min = elapsed;
            if (Max < elapsed) Max = elapsed;
        }

        elapsedSum += elapsed;
    }

    // on complete.
    public void TrySetStatus(ExecutionStatus status)
    {
        if (this.ExecutionStatus == ExecutionStatus.Running)
        {
            this.executeCompleted = DateTime.UtcNow;
            this.ExecutionStatus = status;
        }
    }
}
