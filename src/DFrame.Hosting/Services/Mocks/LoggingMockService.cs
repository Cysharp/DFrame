using DFrame.Hosting.Data;
using DFrame.Hosting.Infrastructure;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using ZLogger;

namespace DFrame.Hosting.Services
{
    public class LoggingMockService : ILoggingService
    {
        private IExecuteContext? _executeContext;
        private readonly ILogger _logger;

        public IExecuteLogProcessor ExecuteLogProcessor { get; }

        public LoggingMockService(LogProcessorOptions options)
        {
            ExecuteLogProcessor = new ExecuteLogProcessor(options);

            //log
            var loggerFactory = LoggerFactory.Create(x =>
            {
                x.SetMinimumLevel(ExecuteLogProcessor.LogLevel);
                x.AddZLoggerLogProcessor(ExecuteLogProcessor);
            });
            _logger = loggerFactory.CreateLogger("mock");
        }

        private void GenerateLogMockData()
        {
            _logger.ZLogInformation("log message foo 1");
            _logger.ZLogInformation("log message bar 2");
            _logger.ZLogInformation("log message hoge 3");
        }

        private void GenerateFailureMockData()
        {
            _logger.ZLogCritical(new HttpRequestException(), "404 not found piyo");
            _logger.ZLogCritical(new HttpRequestException(), "404 not found nanika ");
            _logger.ZLogCritical(new HttpRequestException(), "404 not found hauhau");
        }

        public void RegisterContext(IExecuteContext executeContext)
        {
            _executeContext = executeContext;
        }

        public LogMessage[] GetLogs()
        {
            GenerateLogMockData();

            return ExecuteLogProcessor.GetAll();
        }

        public FailureMessage[] GetExceptionLogs()
        {
            GenerateFailureMockData();

            var g = ExecuteLogProcessor.GetExceptions().GroupBy(x => new { x.Method, x.Path, x.Message });
            return ExecuteLogProcessor.GetExceptions();
        }

        public void Clear()
        {
            ExecuteLogProcessor.Clear();
        }
    }
}
