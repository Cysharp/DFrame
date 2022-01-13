using System;
using System.Diagnostics;
using System.Reflection;
using System.Threading.Tasks;
using DFrame.Hosting.Data;
using DFrame.Hosting.Infrastructure;
using DFrame.Hosting.Internal;
using DFrame.Profiler;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ZLogger;

namespace DFrame.Hosting.Services
{
    public class ExecuteService
    {
        private readonly ILoggingService _loggingService;
        private readonly IExecuteLogProcessor _errorNotifier;
        private readonly IServiceProvider _serviceProvider;

        private ExecuteContext _executeContext = default;
        public ExecuteContext ExecuteContext => _executeContext;

        public Action<string>? OnUpdateStatus { get; set; }
        public Action<IExecuteContext>? OnRegisterContext { get; set; }

        public ExecuteService(ILoggingService loggingService, IServiceProvider serviceProvider)
        {
            _loggingService = loggingService;
            _errorNotifier = new ExecuteLogProcessor(new LogProcessorOptions
            {
                LogLevel = LogLevel.Error,
            });
            _serviceProvider = serviceProvider;
        }

        public ExecuteContext CreateContext(ExecuteData executeData)
        {
            var contextId = Guid.NewGuid().ToString();
            var context = new ExecuteContext(contextId, executeData);
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

            var sw = ValueStopwatch.StartNew();

            // todo: specify IScalingProvider
            // run dframe
            await Host.CreateDefaultBuilder(_executeContext.Argument.Arguments)
                .ConfigureLogging(logging =>
                {
                    logging.ClearProviders();
                    logging.SetMinimumLevel(_loggingService.ExecuteLogProcessor.LogLevel);
                    logging.AddZLoggerLogProcessor(_loggingService.ExecuteLogProcessor);
                    logging.AddZLoggerLogProcessor(_errorNotifier);
                    // todo: remove console logger?
                    logging.AddZLoggerConsole();
                })
                .RunDFrameLoadTestingAsync(_executeContext.Argument.Arguments!, new DFrameOptions(_executeContext.Argument.HostAddress, 12345));

            var duration = sw.Elapsed;

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

            // record
            using (var scope = _serviceProvider.CreateScope())
            {
                // todo: requests and errors will obtain after progress implementation
                var profiler = scope.ServiceProvider.GetService<IDFrameProfiler>();
                await profiler.InsertAsync(_executeContext.ExecuteId, _executeContext.Argument.WorkloadName, _executeContext.Argument.Arguments, 0, 0, duration, default);
            }
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
