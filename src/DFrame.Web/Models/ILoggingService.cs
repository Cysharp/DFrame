using DFrame.Web.Data;
using DFrame.Web.Infrastructure;
using System.Linq;

namespace DFrame.Web.Models
{
    public interface ILoggingService
    {
        IExecuteLogProcessor ExecuteLogProcessor { get; }

        void RegisterContext(IExecuteContext executeContext);
        LogMessage[] GetLogs();
        Failure[] GetExceptionLogs();
        void Clear();
    }

    public class LoggingService : ILoggingService
    {
        private IExecuteContext _executeContext;

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

        public Failure[] GetExceptionLogs()
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
