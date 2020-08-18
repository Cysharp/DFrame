using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DFrame.Web.Data
{
    public interface ISummaryService
    {
        /// <summary>
        /// Get summary
        /// </summary>
        /// <returns></returns>
        Task<Summary> GetSummaryAsync();

        Summary Summary { get; }
    }

    public class SummaryMockService : ISummaryService
    {
        private readonly IStatisticsService _statisticsService;
        private readonly IWorkersService _workersService;

        public Summary Summary { get; private set; } = new Summary();

        public SummaryMockService(IStatisticsService statisticsService, IWorkersService workersService)
        {
            _statisticsService = statisticsService;
            _workersService = workersService;
        }

        public Task<Summary> GetSummaryAsync()
        {
            var workers = _workersService.Cache;
            var summary = new Summary
            {
                Host = _statisticsService.HostAddress,
                Status = "RUNNING",
                Workers = workers.Length,
                Rps = _statisticsService.Aggregated.CurrentRps,
                Failures = _statisticsService.Aggregated.Fails == 0
                    ? 0
                    : (double)_statisticsService.Aggregated.Fails / (double)_statisticsService.Aggregated.Requests * 100,
            };

            return Task.FromResult(summary);
        }
    }
}
