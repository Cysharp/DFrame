using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DFrame.Web.Data
{
    public interface ISummaryService
    {
        Summary Summary { get; }

        Task IniatilizeAsync();
        void UpdateStatus(string status);
        void UpdateHostAddress(string hostAddress);
        void UpdateStatistics(Statistic statistic);
        void UpdateWorker(Worker[] workers);
    }

    public class SummaryMockService : ISummaryService
    {
        private Summary _summary;
        public Summary Summary => _summary;

        private IStatisticsService _statisticsService;
        private IWorkersService _workersService;

        public SummaryMockService(IStatisticsService statisticsService, IWorkersService workersService)
        {
            _summary = new Summary();

            _statisticsService = statisticsService;
            _statisticsService.OnUpdateHostAddress += UpdateHostAddress;
            _statisticsService.OnUpdateStatistics += UpdateStatistics;

            _workersService = workersService;
            _workersService.OnUpdateWorker += UpdateWorker;
            UpdateStatus("RUNNING");
        }

        public Task IniatilizeAsync()
        {
            return Task.CompletedTask;
        }

        public void UpdateStatus(string status)
        {
            _summary.Status = status;
        }

        public void UpdateHostAddress(string hostAddress)
        {
            _summary.Host = hostAddress;
        }

        public void UpdateStatistics(Statistic statistic)
        {
            _summary.Rps = statistic.CurrentRps;
            _summary.Failures = statistic.Fails == 0
                ? 0
                : (double)statistic.Fails / (double)statistic.Requests * 100;
        }

        public void UpdateWorker(Worker[] workers)
        {
            _summary.Workers = workers.Length;
        }
    }
}
