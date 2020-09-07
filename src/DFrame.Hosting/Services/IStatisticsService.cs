using DFrame.Hosting.Data;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace DFrame.Hosting.Services
{
    public interface IStatisticsService<T>
    {
        Action<T>? OnUpdateStatistics { get; set; }

        /// <summary>
        /// Get statistics
        /// </summary>
        /// <returns></returns>
        Task<(T[] statistics, T aggregated)> GetStatisticsAsync();
    }

    public class AbStatisticsService : IStatisticsService<AbStatistic>
    {
        public Action<AbStatistic>? OnUpdateStatistics { get; set; }

        private AbStatistic? _statistics;

        public AbStatisticsService()
        {
            DFrame.ReportNotifier.OnReportOutput.OnPublished += ReportPublished;
        }

        public Task<(AbStatistic[] statistics, AbStatistic aggregated)> GetStatisticsAsync()
        {
            return Task.FromResult((new AbStatistic[] { }, _statistics!));
        }

        private void ReportPublished(AbReport report)
        {
            _statistics = new AbStatistic
            {
                ScenarioName = report.ScenarioName,
                ScalingType = report.ScalingType,
                RequestCount = report.RequestCount,
                ProcessCount = report.ProcessCount,
                WorkerPerProcess = report.WorkerPerProcess,
                ExecutePerWorker = report.ExecutePerWorker,
                ConcurrencyLevel = report.Concurrency,
                CompleteRequests = report.CompleteRequests,
                FailedRequests = report.FailedRequests,
                TimeTaken = report.TimeTaken,
                RequestsPerSeconds = report.TotalRequests / report.TimeTaken,
                TimePerRequest = report.Concurrency * report.TimePerRequest,
                TimePerRequest2 = report.TimePerRequest,
                Percentiles = report.Percentiles.Select(x => (x.Range, x.Value, x.Note)).ToArray(),
            };

            OnUpdateStatistics?.Invoke(_statistics);
        }
    }
}
