using ConsoleAppFramework;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DFrame
{
    public static class DFrameLoadTestingHostBuilderExtensions
    {
        public static async Task RunDFrameLoadTestingAsync(this IHostBuilder hostBuilder, string[] args, DFrameOptions options)
        {
            options.OnExecuteResult = SummaryResult;

            await hostBuilder.RunDFrameAsync(args, options);
        }

        static void SummaryResult(ExecuteResult[] results, DFrameOptions options, ExecuteScenario executeScenario)
        {
            // TODO: Logger
            if (!results.Any())
            {
                // canceled
                Console.WriteLine("No execution result found, quit result report.");
                return;
            }
            Console.WriteLine("Show Load Testing result report.");

            // Output req/sec and other calcutlation report.
            OutputReportAb(results, options, executeScenario);
        }

        /// <summary>
        /// ApacheBench like reports
        /// </summary>
        /// <param name="results"></param>
        /// <param name="options"></param>
        static void OutputReportAb(ExecuteResult[] results, DFrameOptions options, ExecuteScenario executeScenario)
        {
            var scalingType = options.ScalingProvider.GetType().Name;
            var abReport = new AbReport(results, executeScenario, scalingType);

            Console.WriteLine(abReport.ToString());
        }
    }

    public class AbReport
    {
        public string ScalingType { get; }
        public string ScenarioName { get; }
        public int RequestCount { get; }
        public int ProcessCount { get; }
        public int WorkerPerProcess { get; }
        public int ExecutePerWorker { get; }
        public int Concurrency { get; }
        public int CompleteRequests { get; }
        public int FailedRequests { get; }
        public double TimeTaken { get; }
        public int TotalRequests { get; }
        public double TimePerRequest { get; }
        public PercentileData[] Percentiles { get; }

        public class PercentileData
        {
            public int Range { get; }
            public int Value { get; set; }
            public string Note { get; set; }
            public PercentileData(int range, int value, string note)
            {
                Range = range;
                Value = value;
                Note = note;
            }
        }

        public AbReport(ExecuteResult[] results, ExecuteScenario executeScenario, string scalingType)
        {
            ScalingType = scalingType;
            ScenarioName = executeScenario.ScenarioName;
            RequestCount = executeScenario.ProcessCount * executeScenario.WorkerPerProcess * executeScenario.ExecutePerWorker;
            Concurrency = executeScenario.WorkerPerProcess;
            TotalRequests = results.Length;
            CompleteRequests = results.Where(x => !x.HasError).Count();
            FailedRequests = results.Where(x => x.HasError).Count();

            // Time to complete all requests.
            // * Get sum of IWorkerReciever.Execute time on each workerId, max execution time will be actual execution time.
            TimeTaken = results.GroupBy(x => x.WorkerId).Select(xs => xs.Sum(x => x.Elapsed.TotalSeconds)).Max();
            // The average time spent per request. The first value is calculated with the formula `concurrency * timetaken * 1000 / done` while the second value is calculated with the formula `timetaken * 1000 / done`
            TimePerRequest = TimeTaken * 1000 / RequestCount;

            // percentile requires sort before calculate
            var sortedResultsElapsedMs = results.Select(x => x.Elapsed.TotalMilliseconds).OrderBy(x => x).ToArray();
            var percecs = new[] { 0.5, 0.66, 0.75, 0.80, 0.90, 0.95, 0.98, 0.99, 1.00 };
            Percentiles = percecs.Select((x, i) =>
            {
                var percent = (int)(x * 100);
                var value = (int)MatchUtils.Percentile(sortedResultsElapsedMs, x);
                return i != percecs.Length - 1
                    ? new PercentileData(percent, value, "")
                    : new PercentileData(percent, value, "(longest request)");
            })
            .ToArray();
        }

        public override string ToString()
        {
            return $@"Finished {RequestCount} requests

Scaling Type:           {ScalingType}
Scenario Name:          {ScenarioName}

Request count:          {RequestCount}
{nameof(ProcessCount)}:           {ProcessCount}
{nameof(WorkerPerProcess)}:       {WorkerPerProcess}
{nameof(ExecutePerWorker)}:       {ExecutePerWorker}
Concurrency level:      {Concurrency}
Complete requests:      {CompleteRequests}
Failed requests:        {FailedRequests}

Time taken for tests:   {TimeTaken:F2} seconds
Requests per seconds:   {TotalRequests / TimeTaken:F2} [#/sec] (mean)
Time per request:       {Concurrency * TimePerRequest:F2} [ms] (mean)
Time per request:       {TimePerRequest:F2} [ms] (mean, across all concurrent requests)

Percentage of the requests served within a certain time (ms)
{string.Join("\n", Percentiles.Select(x => string.IsNullOrWhiteSpace(x.Note)
    ? $"{x.Range,3}%      {x.Value}"
    : $"{x.Range,3}%      {x.Value} {x.Note}"
))}";
        }
    }

    public static class MatchUtils
    {
        /// <summary>
        /// Calculate Percentile with Interpolation.
        /// </summary>
        /// <param name="sortedSequence"></param>
        /// <param name="percentile"></param>
        /// <returns></returns>
        public static double Percentile(double[] sortedSequence, double percentile)
        {
            var n = sortedSequence.Length;
            var realIndex = Round((n + 1) * percentile) - 1;
            var rank = (int)realIndex;
            var flac = realIndex - rank;
            if (rank >= n)
            {
                // last
                return sortedSequence[n - 1];
            }
            else if (flac == 0)
            {
                // when index match to rank
                return sortedSequence[rank];
            }
            else if (rank + 1 < n)
            {
                // calculate interpolation
                return Round(sortedSequence[rank] + (sortedSequence[rank + 1] - sortedSequence[rank]) * flac);
            }
            else
            {
                return sortedSequence[rank];
            }
        }

        private static double Round(double value, int digit = 2)
        {
            return Math.Round(value, digit, MidpointRounding.AwayFromZero);
        }
    }
}
