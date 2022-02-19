using DFrame.Internal;
using System.Runtime.Serialization;

namespace DFrame.Controller;

public enum ExecutionStatus
{
    Running,
    Succeed,
    Failed,
    // Canceled
}

// TODO:make serializable
public class SummarizedExecutionResult
{
    DateTime? executeBegin;
    DateTime? executeCompleted;
    TimeSpan elapsedSum;

    FixedSizeList<TimeSpan>? elapsedValues;


    [IgnoreDataMember]
    internal IWorkerReceiver? executeBroadcasterToSelf = default!;
    [IgnoreDataMember]
    internal long[] executeCountPerWorkload = default!;

    public WorkerId WorkerId { get; }
    public int WorkloadCount { get; }
    public ExecutionStatus ExecutionStatus { get; private set; }

    public bool Error { get; private set; }
    public string? ErrorMessage { get; private set; }
    public long CompleteCount { get; private set; }
    public long SucceedCount { get; private set; }
    public long ErrorCount { get; set; }
    public TimeSpan TotalElapsed => elapsedSum;
    public TimeSpan Latest { get; private set; }
    public TimeSpan Min { get; private set; }
    public TimeSpan Max { get; private set; }
    public TimeSpan Avg => (SucceedCount == 0) ? TimeSpan.Zero : TimeSpan.FromTicks(elapsedSum.Ticks / SucceedCount);
    public double Rps => (TotalElapsed.TotalSeconds == 0 || (executeBegin == null)) ? 0 : (SucceedCount / RunningTime.TotalSeconds);

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

    // Calc from elapsedValues when completed.
    public TimeSpan? Median { get; private set; }
    public TimeSpan? Percentile90 { get; private set; }
    public TimeSpan? Percentile95 { get; private set; }

    public SummarizedExecutionResult(WorkerId workerId, int workloadCount)
    {
        this.elapsedValues = new FixedSizeList<TimeSpan>(100000); // TODO: from DFrameControllerOptions.
        this.WorkerId = workerId;
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
        if (this.ExecutionStatus != ExecutionStatus.Running) return;

        CompleteCount++;
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
        elapsedValues?.AddLast(elapsed);
    }

    // on complete.
    public bool TrySetStatus(ExecutionStatus status)
    {
        if (this.ExecutionStatus == ExecutionStatus.Running)
        {
            this.executeCompleted = DateTime.UtcNow;
            this.ExecutionStatus = status;

            if (elapsedValues != null)
            {
                var array = elapsedValues.ToArray();
                Array.Sort(array);
                elapsedValues = null;

                if (array.Length > 0)
                {
                    if (array.Length == 1)
                    {
                        Median = Percentile90 = Percentile95 = array[0];
                    }
                    else
                    {
                        // Calc Median
                        if (array.Length % 2 == 0)
                        {
                            var i = array.Length / 2;
                            var i2 = i - 1;
                            Median = TimeSpan.FromTicks((array[i].Ticks + array[i2].Ticks) / 2);
                        }
                        else
                        {
                            Median = array[array.Length / 2];
                        }

                        // Calc percentile
                        Percentile90 = Percentile(array, 0.9);
                        Percentile95 = Percentile(array, 0.95);
                    }
                }
            }
            return true;
        }
        return false;
    }

    // values is sorted.
    static TimeSpan Percentile(TimeSpan[] values, double percentile)
    {
        var realIndex = percentile * (values.Length - 1.0);
        var index = (int)realIndex;
        var frac = realIndex - index;
        if (index + 1 < values.Length)
        {
            return values[index] * (1 - frac) + values[index + 1] * frac;
        }
        else
        {
            return values[index];
        }
    }
}
