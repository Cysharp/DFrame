using DFrame.Hosting.Data;
using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace DFrame.Hosting.Models
{
    public interface IStatisticsService
    {
        Action<Statistic>? OnUpdateStatistics { get; set; }

        void RegisterContext(IExecuteContext executeContext);
        /// <summary>
        /// Get statistics
        /// </summary>
        /// <returns></returns>
        Task<(Statistic[] statistics, Statistic aggregated)> GetStatisticsAsync();
    }

    public class StatisticsService : IStatisticsService
    {
        public Action<Statistic>? OnUpdateStatistics { get; set; }

        private Statistic[]? _statistics;

        public StatisticsService()
        {
            DFrame.ReportNotifier.OnReportOutput.OnPublished = ReportPublished;
        }

        public Task<(Statistic[] statistics, Statistic aggregated)> GetStatisticsAsync()
        {
            throw new NotImplementedException();
        }

        public void RegisterContext(IExecuteContext executeContext)
        {
            throw new NotImplementedException();
        }

        private void ReportPublished(AbReport report)
        {
        }
    }
}
