using DFrame.Web.Infrastructure;

namespace DFrame.Web.Models
{
    public interface ILoggingService
    {
        IExecuteLogProcessor ExecuteLogProcessor { get; }

        string[] GetLogs();
        string[] GetExceptionLogs();
        void Clear();
    }

    public class LoggingService : ILoggingService
    {
        public IExecuteLogProcessor ExecuteLogProcessor { get; }

        public LoggingService(LogProcessorOptions options)
        {
            ExecuteLogProcessor = new ExecuteLogProcessor(options);
        }

        public string[] GetLogs()
        {
            return ExecuteLogProcessor.GetAll();
        }

        public string[] GetExceptionLogs()
        {
            return ExecuteLogProcessor.GetExceptions();
        }

        public void Clear()
        {
            ExecuteLogProcessor.Clear();
        }
    }
}
