using DFrame.Hosting.Data;
using DFrame.Hosting.Infrastructure;
using System.Linq;

namespace DFrame.Hosting.Models
{
    public interface ILoggingService
    {
        IExecuteLogProcessor ExecuteLogProcessor { get; }

        void RegisterContext(IExecuteContext executeContext);
        LogMessage[] GetLogs();
        FailureMessage[] GetExceptionLogs();
        void Clear();
    }

    public class LoggingService : ILoggingService
    {
        private IExecuteContext? _executeContext;

        public IExecuteLogProcessor ExecuteLogProcessor { get; }

        public LoggingService(LogProcessorOptions options)
        {
            ExecuteLogProcessor = new ExecuteLogProcessor(options);
        }

        public void RegisterContext(IExecuteContext executeContext)
        {
            _executeContext = executeContext;
        }

        public LogMessage[] GetLogs()
        {
            return ExecuteLogProcessor.GetAll();
        }

        public FailureMessage[] GetExceptionLogs()
        {
            var g = ExecuteLogProcessor.GetExceptions().GroupBy(x => new { x.Method, x.Path, x.Message });
            return ExecuteLogProcessor.GetExceptions();
        }

        public void Clear()
        {
            ExecuteLogProcessor.Clear();
        }
    }
}
