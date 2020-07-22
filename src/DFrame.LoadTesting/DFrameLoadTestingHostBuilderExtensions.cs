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
            Console.WriteLine("Show Load Testing result report.");

            // Output req/sec and other calcutlation report.
            AbReport(results, options, executeScenario);
        }

        /// <summary>
        /// ApacheBench like reports
        /// </summary>
        /// <param name="results"></param>
        /// <param name="options"></param>
        static void AbReport(ExecuteResult[] results, DFrameOptions options, ExecuteScenario executeScenario)
        {
            var requestCount = executeScenario.NodeCount * executeScenario.WorkerPerNode * executeScenario.ExecutePerWorker;
            var concurrency = executeScenario.WorkerPerNode;
            var totalRequests = results.Length;
            var completeRequests = results.Where(x => !x.HasError).Count();
            var failedRequests = results.Where(x => x.HasError).Count();

            // Time to complete all requests.
            // * Get sum of IWorkerReciever.Execute time on each workerId, max execution time will be actual execution time.
            var timeTaken = results.GroupBy(x => x.WorkerId).Select(xs => xs.Sum(x => x.Elapsed.TotalSeconds)).Max();
            // The average time spent per request. The first value is calculated with the formula `concurrency * timetaken * 1000 / done` while the second value is calculated with the formula `timetaken * 1000 / done`
            var timePerRequest = timeTaken * 1000 / requestCount;

            // percentile requires sort before calculate
            var sortedResultsElapsedMs = results.Select(x => x.Elapsed.TotalMilliseconds).OrderBy(x => x).ToArray();
            var percecs = new[] { 0.5, 0.66, 0.75, 0.80, 0.90, 0.95, 0.98, 0.99, 1.00 };
            var percentiles = percecs.Select((x, i) =>
            {
                var percent = (int)(x * 100);
                var percentile = (int)Percentile(sortedResultsElapsedMs, x);
                return i != percecs.Length - 1 
                    ? $"{percent,3}%      {percentile}"
                    : $"{percent,3}%      {percentile} (longest request)";
            })
            .ToArray();

            Console.WriteLine($@"Finished {requestCount} requests

Scaling Type:           {options.ScalingProvider.GetType().Name}

Request count:          {requestCount}
NodeCount:              {executeScenario.NodeCount}
WorkerPerNode:          {executeScenario.WorkerPerNode}
ExecutePerWorker:       {executeScenario.ExecutePerWorker}
Concurrency level:      {concurrency}
Complete requests:      {completeRequests}
Failed requests:        {failedRequests}

Time taken for tests:   {timeTaken:F2} seconds
Requests per seconds:   {totalRequests / timeTaken:F2} [#/sec] (mean)
Time per request:       {concurrency * timePerRequest:F2} [ms] (mean)
Time per request:       {timePerRequest:F2} [ms] (mean, across all concurrent requests)

Percentage of the requests served within a certain time (ms)
{string.Join("\n", percentiles)}");
        }

        /// <summary>
        /// Calculate Percentile with Interpolation.
        /// </summary>
        /// <param name="sortedSequence"></param>
        /// <param name="percentile"></param>
        /// <returns></returns>
        private static double Percentile(double[] sortedSequence, double percentile)
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
