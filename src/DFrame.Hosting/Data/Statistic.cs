using System;

namespace DFrame.Hosting.Data
{
    public class AbStatistic
    {
        public string? ScalingType { get; set; }
        public string? WorkloadName { get; set; }
        public int RequestCount { get; set; }
        public int WorkerCount { get; set; }
        public int WorkloadPerWorker { get; set; }
        public int ExecutePerWorkload { get; set; }
        public int ConcurrencyLevel { get; set; }
        public int CompleteRequests { get; set; }
        public int FailedRequests { get; set; }
        public double TimeTaken { get; set; }
        public double RequestsPerSeconds { get; set; }
        public double TimePerRequest { get; set; }
        public double TimePerRequest2 { get; set; }
        public (int percentile, int value, string? note)[]? Percentiles { get; set; }
    }

    public class Statistic
    {
        public string Method { get; set; }
        public string Name { get; set; }
        public int Requests { get; set; }
        public int Fails { get; set; }
        public double Median { get; set; }
        public double Percentile90 { get; set; }
        public double Average { get; set; }
        public double Min { get; set; }
        public double Max { get; set; }
        public double AverageSize { get; set; }
        public double CurrentRps { get; set; }
        public double CurrentFailuresPerSec { get; set; }
    }
}
