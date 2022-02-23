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

[DataContract]
public class SummarizedExecutionResult
{
    [IgnoreDataMember]
    FixedSizeList<TimeSpan>? elapsedValues;

    [IgnoreDataMember]
    internal IWorkerReceiver? executeBroadcasterToSelf = default!;
    [IgnoreDataMember]
    internal long[] executeCountPerWorkload = default!;

    [DataMember]
    DateTime? ExecuteBegin { get; set; }
    [DataMember]
    DateTime? ExecuteCompleted { get; set; }
    [DataMember]
    public WorkerId WorkerId { get; set; }
    [DataMember]
    public int WorkloadCount { get; set; }
    [DataMember]
    public IReadOnlyList<KeyValuePair<string, string>> Metadata { get; set; }
    
    [DataMember]
    public Dictionary<WorkloadId, Dictionary<string, string>?>? Results { get; set; }
    [DataMember]
    public ExecutionStatus ExecutionStatus { get; set; }
    [DataMember]
    public bool Error { get; set; }
    [DataMember]
    public string? ErrorMessage { get; set; }
    [DataMember]
    public long CompleteCount { get; set; }
    [DataMember]
    public long SucceedCount { get; set; }
    [DataMember]
    public long ErrorCount { get; set; }
    [DataMember]
    public TimeSpan TotalElapsed { get; set; }
    [DataMember]
    public TimeSpan Latest { get; set; }
    [DataMember]
    public TimeSpan Min { get; set; }
    [DataMember]
    public TimeSpan Max { get; set; }

    // Calc from elapsedValues when completed.
    [DataMember]
    public TimeSpan? Median { get; set; }
    [DataMember]
    public TimeSpan? Percentile90 { get; set; }
    [DataMember]
    public TimeSpan? Percentile95 { get; set; }

    [IgnoreDataMember]
    public TimeSpan Avg => (SucceedCount == 0) ? TimeSpan.Zero : TimeSpan.FromTicks(TotalElapsed.Ticks / SucceedCount);
    [IgnoreDataMember]
    public double Rps => (TotalElapsed.TotalSeconds == 0 || (ExecuteBegin == null)) ? 0 : (SucceedCount / RunningTime.TotalSeconds);

    [IgnoreDataMember]
    public TimeSpan RunningTime
    {
        get
        {
            if (ExecuteBegin == null)
            {
                return TimeSpan.Zero;
            }

            if (ExecuteCompleted == null)
            {
                return DateTime.UtcNow - ExecuteBegin.Value;
            }

            return ExecuteCompleted.Value - ExecuteBegin.Value;
        }
    }

    // for serialize.
    public SummarizedExecutionResult()
    {
        Metadata = Array.Empty<KeyValuePair<string, string>>();
    }

    public SummarizedExecutionResult(WorkerId workerId, int workloadCount, IReadOnlyList<KeyValuePair<string, string>> metadata, DFrameControllerOptions options)
    {
        this.elapsedValues = new FixedSizeList<TimeSpan>(options.CompleteElapsedBufferCount);
        this.WorkerId = workerId;
        this.WorkloadCount = workloadCount;
        this.ExecutionStatus = ExecutionStatus.Running;
        this.Metadata = metadata;
    }

    public void InitExecuteBeginTime(DateTime executeBegin)
    {
        if (this.ExecuteBegin == null)
        {
            this.ExecuteBegin = executeBegin;
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

        TotalElapsed += elapsed;
        elapsedValues?.AddLast(elapsed);
    }

    public void Add(BatchedExecuteResult result)
    {
        if (this.ExecutionStatus != ExecutionStatus.Running) return;

        CompleteCount += result.BatchedElapsed.Count;
        SucceedCount += result.BatchedElapsed.Count;

        foreach (var item in result.BatchedElapsed)
        {
            var elapsed = TimeSpan.FromTicks(item);

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

            TotalElapsed += elapsed;
            elapsedValues?.AddLast(elapsed);
        }
    }

    // on complete.
    public bool TrySetStatus(ExecutionStatus status, Dictionary<WorkloadId, Dictionary<string, string>?>? results)
    {
        if (this.ExecutionStatus == ExecutionStatus.Running)
        {
            this.ExecuteCompleted = DateTime.UtcNow;
            this.ExecutionStatus = status;
            this.Results = results;

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
