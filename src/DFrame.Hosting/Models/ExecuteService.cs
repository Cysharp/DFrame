using System;
using System.Threading.Tasks;
using DFrame.Hosting.Data;
using DFrame.Hosting.Infrastructure;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ZLogger;

namespace DFrame.Hosting.Models
{
    public class ExecuteService
    {
        private readonly ISummaryService _summaryService;
        private readonly IStatisticsService _statisticsService;
        private readonly ILoggingService _loggingService;
        private readonly IExecuteLogProcessor _errorNotifier;

        private ExecuteContext _executeContext = default;
        public ExecuteContext ExecuteContext => _executeContext;

        public ExecuteService(ISummaryService summaryService, IStatisticsService statisticsService, ILoggingService loggingService)
        {
            _summaryService = summaryService;
            _statisticsService = statisticsService;
            _loggingService = loggingService;
            _errorNotifier = new ExecuteLogProcessor(new LogProcessorOptions
            {
                LogLevel = LogLevel.Error,
            });
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
            RegisterContext(context);

            return context;
        }

        public async Task ExecuteAsync()
        {
            _errorNotifier.Clear();

            // update status
            await _executeContext.ExecuteAsync();
            _summaryService.UpdateStatus(_executeContext.Status);

            // run dframe
            await Host.CreateDefaultBuilder(_executeContext.ExecuteArgument.Arguments)
                .ConfigureLogging(logging =>
                {
                    logging.ClearProviders();
                    logging.SetMinimumLevel(_loggingService.ExecuteLogProcessor.LogLevel);
                    logging.AddZLoggerLogProcessor(_loggingService.ExecuteLogProcessor);
                    logging.AddZLoggerLogProcessor(_errorNotifier);
                    // todo: remove console logger?
                    logging.AddZLoggerConsole();
                })
                .RunDFrameLoadTestingAsync(_executeContext.ExecuteArgument.Arguments, new DFrameOptions(_executeContext.HostAddress, 12345));

            // update status
            if (_errorNotifier.GetExceptions().Length == 0)
            {
                await _executeContext.StopAsync();
            }
            else
            {
                await _executeContext.ErrorAsync();
            }
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

        private void RegisterContext(IExecuteContext context)
        {
            _summaryService?.RegisterContext(context);
            _statisticsService?.RegisterContext(context);
            _loggingService?.RegisterContext(context);
        }
    }
}
