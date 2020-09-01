using DFrame.Hosting.Data;

namespace DFrame.Hosting.Models
{
    public interface ISummaryService
    {
        Summary Summary { get; }

        void RegisterContext(IExecuteContext executeContext);
        void UpdateStatus(string status);
        void UpdateWorker(int workerCount);
    }

    public class SummaryService : ISummaryService
    {
        private readonly Summary _summary;
        public Summary Summary => _summary;

        private readonly IWorkersService _workersService;

        public SummaryService(ExecuteService executeService, IWorkersService workersService)
        {
            _summary = new Summary();

            executeService.OnRegisterContext += RegisterContext;
            executeService.OnUpdateStatus += UpdateStatus;

            _workersService = workersService;
            _workersService.OnWorkerUpdate += UpdateWorker;
        }

        public void RegisterContext(IExecuteContext executeContext)
        {
            _summary.Host = executeContext.HostAddress;
            _summary.ExecuteId = executeContext.ExecuteId;
        }

        public void UpdateStatus(string status)
        {
            _summary.Status = status;
        }

        public void UpdateWorker(int workerCount)
        {
            _summary.Workers = workerCount;
        }
    }
}
