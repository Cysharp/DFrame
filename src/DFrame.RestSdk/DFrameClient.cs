using System.Net.Http.Json;
using System.Runtime.Serialization;
using UnitGenerator;

namespace DFrame.RestSdk;

public class DFrameClient
{
    readonly HttpClient httpClient;
    readonly string rootAddress;

    string? urlIsRunning;
    string? urlConnections;
    string? urlLatestResult;
    string? urlResultsCount;
    string? urlResultsList;
    string? urlCancel;
    string? urlRequest;
    string? urlRepeat;
    string? urlDuration;
    string? urlInfinite;

    public DFrameClient(string rootAddress)
        : this(new HttpClient(), rootAddress)
    {
    }

    public DFrameClient(HttpClient httpClient, string rootAddress)
    {
        this.httpClient = httpClient;
        this.rootAddress = rootAddress.TrimEnd('/');
    }

    // Get Apis

    public async Task<int> GetConnectionCountAsync(CancellationToken cancellationToken = default)
    {
        var response = await httpClient.GetAsync(urlConnections ??= $"{rootAddress}/api/connections", cancellationToken);
        return await response.EnsureSuccessStatusCode().Content.ReadFromJsonAsync<int>();
    }

    public async Task<bool> IsRunningAsync(CancellationToken cancellationToken = default)
    {
        var response = await httpClient.GetAsync(urlIsRunning ??= $"{rootAddress}/api/isrunning", cancellationToken);
        return await response.EnsureSuccessStatusCode().Content.ReadFromJsonAsync<bool>();
    }

    public async Task<ExecutionResult?> GetLatestResultAsync(CancellationToken cancellationToken = default)
    {
        var response = await httpClient.GetAsync(urlLatestResult ??= $"{rootAddress}/api/latestresult", cancellationToken);
        return await response.EnsureSuccessStatusCode().Content.ReadFromJsonAsync<ExecutionResult>();
    }

    public async Task<int> GetResultsCountAsync(CancellationToken cancellationToken = default)
    {
        var response = await httpClient.GetAsync(urlResultsCount ??= $"{rootAddress}/api/resultscount", cancellationToken);
        return await response.EnsureSuccessStatusCode().Content.ReadFromJsonAsync<int>();
    }

    public async Task<ExecutionSummary[]> GetResultsListAsync(CancellationToken cancellationToken = default)
    {
        var response = await httpClient.GetAsync(urlResultsList ??= $"{rootAddress}/api/resultslist", cancellationToken);
        return (await response.EnsureSuccessStatusCode().Content.ReadFromJsonAsync<ExecutionSummary[]>())!;
    }

    public async Task<ExecutionResult?> GetResultAsync(ExecutionId executionId, CancellationToken cancellationToken = default)
    {
        var response = await httpClient.GetAsync($"{rootAddress}/api/getresult?executionId={executionId}", cancellationToken);
        return (await response.EnsureSuccessStatusCode().Content.ReadFromJsonAsync<ExecutionResult?>());
    }

    // Post Apis

    public async Task CancelAsync(CancellationToken cancellationToken = default)
    {
        var response = await httpClient.PostAsync(urlCancel ??= $"{rootAddress}/api/cancel", null, cancellationToken);
        response.EnsureSuccessStatusCode();
    }

    public async Task<ExecutionSummary> ExecuteRequestAsync(RequestBody body, CancellationToken cancellationToken = default)
    {
        var response = await httpClient.PostAsJsonAsync(urlRequest ??= $"{rootAddress}/api/request", body, cancellationToken);
        return (await response.EnsureSuccessStatusCode().Content.ReadFromJsonAsync<ExecutionSummary>())!;
    }

    public async Task<ExecutionSummary> ExecuteRepeatAsync(RepeatBody body, CancellationToken cancellationToken = default)
    {
        var response = await httpClient.PostAsJsonAsync(urlRepeat ??= $"{rootAddress}/api/repeat", body, cancellationToken);
        return (await response.EnsureSuccessStatusCode().Content.ReadFromJsonAsync<ExecutionSummary>())!;
    }

    public async Task<ExecutionSummary> ExecuteDurationAsync(DurationBody body, CancellationToken cancellationToken = default)
    {
        var response = await httpClient.PostAsJsonAsync(urlDuration ??= $"{rootAddress}/api/duration", body, cancellationToken);
        return (await response.EnsureSuccessStatusCode().Content.ReadFromJsonAsync<ExecutionSummary>())!;
    }

    public async Task<ExecutionSummary> ExecuteInfiniteAsync(InfiniteBody body, CancellationToken cancellationToken = default)
    {
        var response = await httpClient.PostAsJsonAsync(urlInfinite ??= $"{rootAddress}/api/infinite", body, cancellationToken);
        return (await response.EnsureSuccessStatusCode().Content.ReadFromJsonAsync<ExecutionSummary>())!;
    }

    // Utils

    /// <summary>
    /// pollingSpan: 5 seconds. 
    /// </summary>
    public Task WaitUntilCanExecute(CancellationToken cancellationToken = default)
    {
        return WaitUntilCanExecute(TimeSpan.FromSeconds(5), cancellationToken);
    }

    public async Task WaitUntilCanExecute(TimeSpan pollingSpan, CancellationToken cancellationToken = default)
    {
        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var isRunning = await IsRunningAsync(cancellationToken);
            if (!isRunning) return; // ok, can execute.

            await Task.Delay(pollingSpan, cancellationToken);
        }
    }
}

public class ExecutionResult
{
    public ExecutionSummary Summary { get; set; } = default!;
    public SummarizedExecutionResult[] Results { get; set; } = default!;
}


public class RequestBody
{
    public string Workload { get; set; } = default!;
    public Dictionary<string, string?>? Parameters { get; set; }
    public int Concurrency { get; set; }
    public int? Workerlimit { get; set; }
    public long TotalRequest { get; set; }
}

public class RepeatBody
{
    public string Workload { get; set; } = default!;
    public Dictionary<string, string?>? Parameters { get; set; }
    public int Concurrency { get; set; }
    public int? Workerlimit { get; set; }
    public long TotalRequest { get; set; }
    public int IncreaseTotalRequest { get; set; }
    public int IncreaseTotalWorker { get; set; }
    public int RepeatCount { get; set; }
}

public class DurationBody
{
    public string Workload { get; set; } = default!;
    public Dictionary<string, string?>? Parameters { get; set; }
    public int Concurrency { get; set; }
    public int? Workerlimit { get; set; }
    public int ExecuteTimeSeconds { get; set; }
}

public class InfiniteBody
{
    public string Workload { get; set; } = default!;
    public Dictionary<string, string?>? Parameters { get; set; }
    public int Concurrency { get; set; }
    public int? Workerlimit { get; set; }
}

[DataContract]
public class ExecutionSummary
{
    // inits
    [DataMember]
    public string Workload { get; set; } = default!;
    [DataMember]
    public ExecutionId ExecutionId { get; set; }
    [DataMember]
    public int WorkerCount { get; set; }
    [DataMember]
    public int WorkloadCount { get; set; }
    [DataMember]
    public int Concurrency { get; set; }
    [DataMember]
    public Dictionary<string, string> Parameters { get; set; } = default!;
    [DataMember]
    public DateTime StartTime { get; set; }

    // modifiable
    [DataMember]
    public TimeSpan? RunningTime { get; set; }
    [DataMember]
    public long TotalRequest { get; set; } // set in init and after completed
    [DataMember]
    public long? SucceedSum { get; set; }
    [DataMember]
    public long? ErrorSum { get; set; }
    [DataMember]
    public double? RpsSum { get; set; }
}

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
    [DataMember]
    DateTime? ExecuteBegin { get; set; }
    [DataMember]
    DateTime? ExecuteCompleted { get; set; }
    [DataMember]
    public WorkerId WorkerId { get; set; }
    [DataMember]
    public int WorkloadCount { get; set; }
    [DataMember]
    public Dictionary<string, string> Metadata { get; set; }

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
        Metadata = new(0);
    }
}

internal static class GenerateOptions
{
    internal const UnitGenerateOptions Guid = UnitGenerateOptions.MessagePackFormatter | UnitGenerateOptions.ParseMethod | UnitGenerateOptions.Comparable | UnitGenerateOptions.WithoutComparisonOperator;
    internal const UnitGenerateOptions GuidJson = UnitGenerateOptions.MessagePackFormatter | UnitGenerateOptions.ParseMethod | UnitGenerateOptions.Comparable | UnitGenerateOptions.WithoutComparisonOperator | UnitGenerateOptions.JsonConverter | UnitGenerateOptions.JsonConverterDictionaryKeySupport;
}

[UnitOf(typeof(Guid), GenerateOptions.GuidJson)]
public readonly partial struct ExecutionId { }

[UnitOf(typeof(Guid), GenerateOptions.GuidJson)]
public readonly partial struct WorkerId { }

[UnitOf(typeof(Guid), GenerateOptions.GuidJson)]
public readonly partial struct WorkloadId { }