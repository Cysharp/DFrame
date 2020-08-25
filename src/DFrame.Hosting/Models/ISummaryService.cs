using DFrame.Hosting.Data;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;

namespace DFrame.Hosting.Models
{
    public interface ISummaryService
    {
        Summary Summary { get; }

        void RegisterContext(IExecuteContext executeContext);
        void UpdateStatus(string status);
        void UpdateStatistics(Statistic statistic);
        void UpdateWorker(int workerCount);
    }

    public class SummaryService : ISummaryService
    {
        private readonly Summary _summary;
        public Summary Summary => _summary;

        private IExecuteContext _executeContext;
        private readonly IStatisticsService _statisticsService;
        private readonly IWorkersService _workersService;

        public SummaryService(IStatisticsService statisticsService, IWorkersService workersService)
        {
            _summary = new Summary();
            _statisticsService = statisticsService;
            _statisticsService.OnUpdateStatistics += UpdateStatistics;

            _workersService = workersService;
            _workersService.OnWorkerUpdate += UpdateWorker;
        }

        public void RegisterContext(IExecuteContext executeContext)
        {
            _executeContext = executeContext;
            _summary.Host = executeContext.HostAddress;
            _summary.ExecuteId = executeContext.ExecuteId;
        }

        public void UpdateStatus(string status)
        {
            _summary.Status = status;
        }

        public void UpdateStatistics(Statistic statistic)
        {
            _summary.Rps = statistic.CurrentRps;
            _summary.Failures = statistic.Fails == 0
                ? 0
                : (double)statistic.Fails / (double)statistic.Requests * 100;
        }

        public void UpdateWorker(int workerCount)
        {
            _summary.Workers = workerCount;
        }
    }
}
