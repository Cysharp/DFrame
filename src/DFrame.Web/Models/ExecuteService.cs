using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using DFrame.Web.Infrastructure;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ZLogger;

namespace DFrame.Web.Models
{
    public class ExecuteService
    {
        private readonly ILogger<ExecuteService> _logger;
        private readonly ISummaryService _summaryService;
        private readonly IStatisticsService _statisticsService;
        private readonly ILoggingService _loggingService;

        private ExecuteContext _executeContext = default;
        public ExecuteContext ExecuteContext => _executeContext;

        public ExecuteService(ILogger<ExecuteService> logger, ISummaryService summaryService, IStatisticsService statisticsService, ILoggingService loggingService, LogProcessorOptions logOptions)
        {
            _logger = logger;
            _summaryService = summaryService;
            _statisticsService = statisticsService;
            _loggingService = loggingService;
        }

        public ExecuteContext CreateContext(string hostAddress, int processCount, int workerPerProcess, int executePerWorker, string workerName)
        {
            var contextId = Guid.NewGuid().ToString();
            var executeArguments = new ExecuteArgument
            {
                WorkerName = workerName,
                ProcessCount = processCount,
                WorkerPerProcess = workerPerProcess,
                ExecutePerWorker = executePerWorker,
                Arguments = $"--master -processCount {processCount} -workerPerProcess {workerPerProcess} -executePerWorker {executePerWorker} -workerName {workerName}".Split(' '),
            };
            var context = new ExecuteContext(contextId, hostAddress, executeArguments);
            _executeContext = context;

            // register context
            _summaryService.RegisterContext(_executeContext);
            _statisticsService.RegisterContext(_executeContext);

            return context;
        }

        public async Task ExecuteAsync()
        {
            // clear current
            _loggingService.Clear();

            // update context status
            await _executeContext.ExecuteAsync();
            _summaryService.UpdateStatus(_executeContext.Status);

            // run dframe
            await Host.CreateDefaultBuilder(_executeContext.ExecuteArgument.Arguments)
                .ConfigureLogging(logging =>
                {
                    logging.ClearProviders();
                    logging.SetMinimumLevel(_loggingService.ExecuteLogProcessor.LogLevel);
                    logging.AddZLoggerLogProcessor(_loggingService.ExecuteLogProcessor);
                })
                .RunDFrameLoadTestingAsync(_executeContext.ExecuteArgument.Arguments, new DFrameOptions(_executeContext.HostAddress, 12345));

            // update status
            await _executeContext.StopAsync();
            _summaryService.UpdateStatus(_executeContext.Status);
        }

        public async Task StopAsync()
        {
            // update context
            await _executeContext.StopAsync();

            // todo: teardown dframe?

            // update status
            _summaryService.UpdateStatus(_executeContext.Status);
        }
    }
}
