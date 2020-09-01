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
        private readonly ILoggingService _loggingService;
        private readonly IExecuteLogProcessor _errorNotifier;

        private ExecuteContext _executeContext = default;
        public ExecuteContext ExecuteContext => _executeContext;

        public Action<string>? OnUpdateStatus { get; set; }
        public Action<IExecuteContext>? OnRegisterContext { get; set; }

        public ExecuteService(ILoggingService loggingService)
        {
            _loggingService = loggingService;
            _errorNotifier = new ExecuteLogProcessor(new LogProcessorOptions
            {
                LogLevel = LogLevel.Error,
            });
        }

        public ExecuteContext CreateContext(string hostAddress, int processCount, int workerPerProcess, int executePerWorker, string workerName)
        {
            var contextId = Guid.NewGuid().ToString();
            var executeArguments = new ExecuteData
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
            OnRegisterContext?.Invoke(context);

            return context;
        }

        public async Task ExecuteAsync()
        {
            _errorNotifier.Clear();

            // update status
            await _executeContext.ExecuteAsync();
            OnUpdateStatus?.Invoke(_executeContext.Status);

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
                .RunDFrameLoadTestingAsync(_executeContext.ExecuteArgument.Arguments!, new DFrameOptions(_executeContext.HostAddress, 12345));

            // update status
            if (_errorNotifier.GetExceptions().Length == 0)
            {
                await _executeContext.StopAsync();
            }
            else
            {
                await _executeContext.ErrorAsync();
            }
            OnUpdateStatus?.Invoke(_executeContext.Status);
        }

        public async Task StopAsync()
        {
            // update context
            await _executeContext.StopAsync();

            // todo: teardown running dframe?

            // update status
            OnUpdateStatus?.Invoke(_executeContext.Status);
        }
    }
}
