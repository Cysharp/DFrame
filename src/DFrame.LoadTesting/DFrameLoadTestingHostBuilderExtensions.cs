using ConsoleAppFramework;
using Microsoft.Extensions.DependencyInjection;
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
            // TODO:req/secとか色々集計したのを返す。
            // とりあえず集計したらConsole.WriteLine(雑)
            ShowReportAb(results, options, executeScenario);

            //foreach (var item in results)
            //{
            //    Console.WriteLine("Time taken for tests:" + item.Elapsed);
            //}
        }

        /// <summary>
        /// ApacheBench like reports
        /// </summary>
        /// <param name="results"></param>
        /// <param name="options"></param>
        static void ShowReportAb(ExecuteResult[] results, DFrameOptions options, ExecuteScenario executeScenario)
        {
            var requestCount = executeScenario.NodeCount * executeScenario.WorkerPerNode * executeScenario.ExecutePerWorker;
            var concurrentExecCount = executeScenario.WorkerPerNode * executeScenario.ExecutePerWorker;

            // 各workerで実行されたIWorkerReciever.Execute の始まりから終わりまでの計測結果の合計
            var sumElapsedRequestsSec = results.Select(x => x.Elapsed.TotalSeconds).Sum();
            var totalRequests = results.Count();
            var completeRequests = results.Where(x => !x.HasError).Count();
            var failedRequests = results.Where(x => x.HasError).Count();
            var timePerRequest = sumElapsedRequestsSec * 1000 / requestCount;

            // percentile requires sort before calculate
            var sortedResultsElapsedMs = results.Select(x => x.Elapsed.TotalMilliseconds).OrderBy(x => x).ToArray();
            var percecs = new[] { 0.5, 0.66, 0.75, 0.80, 0.90, 0.95, 0.98, 0.99, 1.00 };

            // header
            Console.WriteLine($"Finished {requestCount} requests");
            Console.WriteLine($"");
            Console.WriteLine($"");

            // connection info
            Console.WriteLine($"Server Hostname:        {options.Host}");
            Console.WriteLine($"Server Port:            {options.Port}");
            Console.WriteLine($"");
            Console.WriteLine($"Scaling Type:           {options.ScalingProvider.GetType().Name}");
            Console.WriteLine($"");

            // result summary
            Console.WriteLine($"Request count:          {requestCount}");
            Console.WriteLine($"Concurrentcy count:     {concurrentExecCount}");
            Console.WriteLine($"Time taken for tests:   {sumElapsedRequestsSec:F2} seconds"); // すべてのリクエストが完了するのにかかった時間
            Console.WriteLine($"Complete requests:      {completeRequests}");
            Console.WriteLine($"Failed requests:        {failedRequests}");
            Console.WriteLine($"Requests per seconds:   {totalRequests / sumElapsedRequestsSec:F2} [#/sec] (mean)"); // リクエスト数 / 合計所要時間
            Console.WriteLine($"Time per request:       {concurrentExecCount * timePerRequest:F2} [ms] (mean)"); // 同時実行したリクエストの平均処理時間 = 同時実行数 * 全てのリクエストが完了するのにかかった時間sec * 1000 / 処理したリクエスト数
            Console.WriteLine($"Time per request:       {timePerRequest:F2} [ms] (mean, across all concurrent requests)"); // 1リクエストの平均処理時間 = 全てのリクエストが完了するのにかかった時間sec * 1000 / 処理したリクエスト数
            Console.WriteLine($"");

            // percentile summary
            Console.WriteLine($"Percentage of the requests served within a certain time (ms)");
            for (var i = 0; i < percecs.Length; i++)
            {
                var percec = percecs[i];
                var percent = (int)(percec * 100);
                var percentile = (int)Percentile(sortedResultsElapsedMs, percec);
                if (i != percecs.Length - 1)
                {
                    Console.WriteLine($" {percent,3}%      {percentile}");
                }
                else
                {
                    Console.WriteLine($" {percent,3}%      {percentile} (longest request)");
                }
            }
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
