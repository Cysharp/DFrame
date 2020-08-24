using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
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

        private ExecuteContext _executeContext = default;
        public ExecuteContext ExecuteContext => _executeContext;

        public ExecuteService(ILogger<ExecuteService> logger, ISummaryService summaryService, IStatisticsService statisticsService)
        {
            _logger = logger;
            _summaryService = summaryService;
            _statisticsService = statisticsService;
        }

        public async Task ExecuteAsync(string hostAddress, int processCount, int workerPerProcess, int executePerWorker, string workerName)
        {
            var contextId = Guid.NewGuid().ToString();
            var args = $"--master -processCount {processCount} -workerPerProcess {workerPerProcess} -executePerWorker {executePerWorker} -workerName {workerName}";
            var context = new ExecuteContext(contextId, hostAddress, workerName, args);
            _executeContext = context;

            // register context
            _summaryService.RegisterContext(_executeContext);
            _statisticsService.RegisterContext(_executeContext);

            // update context
            await _executeContext.ExecuteAsync();
            _summaryService.UpdateStatus(_executeContext.Status);

            // run dframe
            await Host.CreateDefaultBuilder(context.Args)
                .ConfigureLogging(logging =>
                {
                    logging.ClearProviders();
                    //logging.SetMinimumLevel(LogLevel.Trace);
                    logging.AddZLoggerConsole(options =>
                    {
                        options.EnableStructuredLogging = false;
                    });
                })
                .RunDFrameLoadTestingAsync(_executeContext.Args, new DFrameOptions(_executeContext.HostAddress, 12345));

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
