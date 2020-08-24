using DFrame.Web.Data;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;

namespace DFrame.Web.Models
{
    public interface ISummaryService
    {
        Summary Summary { get; }

        void RegisterContext(IExecuteContext executeContext);
        void UpdateStatus(string status);
        void UpdateStatistics(Statistic statistic);
        void UpdateWorker(WorkerData[] workers);
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
            _workersService.OnUpdateWorker += UpdateWorker;
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

        public void UpdateWorker(WorkerData[] workers)
        {
            _summary.Workers = workers.Length;
        }
    }
}
