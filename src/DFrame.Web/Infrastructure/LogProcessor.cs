using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ZLogger;

namespace DFrame.Web.Infrastructure
{
    public class LogProcessorOptions
    {
        public LogLevel LogLevel { get; set; } = LogLevel.Information;
        public ZLoggerOptions LoggerOptions { get; set; }
    }

    public interface IExecuteLogProcessor : IAsyncLogProcessor
    {
        LogLevel LogLevel { get; }
        string[] GetAll();
        string[] GetExceptions();
        string Remove();
        string RemoveException();
        void Clear();
    }

    public class ExecuteLogProcessor : IExecuteLogProcessor
    {
        private readonly LogProcessorOptions options;
        private readonly ConcurrentQueue<string> _entryMessages;
        private readonly ConcurrentQueue<string> _exceptionMessages;

        public LogLevel LogLevel => options.LogLevel;

        public ExecuteLogProcessor(LogProcessorOptions options)
        {
            this.options = options;
            _entryMessages = new ConcurrentQueue<string>();
            _exceptionMessages = new ConcurrentQueue<string>();
        }

        public ValueTask DisposeAsync()
        {
            return default;
        }

        public void Post(IZLoggerEntry log)
        {
            if (log.LogInfo.LogLevel >= LogLevel.Error)
            {
                _exceptionMessages.Enqueue(log.FormatToString(options.LoggerOptions, null));
            }
            else
            {
                _entryMessages.Enqueue(log.FormatToString(options.LoggerOptions, null));
            }
        }

        public string[] GetAll()
        {
            var list = new List<string>();
            foreach (var message in _entryMessages)
            {
                list.Add(message);
            }
            return list.ToArray();
        }

        public string[] GetExceptions()
        {
            var list = new List<string>();
            foreach (var message in _exceptionMessages)
            {
                list.Add(message);
            }
            return list.ToArray();
        }

        public string Remove()
        {
            _entryMessages.TryDequeue(out var ret);
            return ret;
        }

        public string RemoveException()
        {
            _exceptionMessages.TryDequeue(out var ret);
            return ret;
        }

        public void Clear()
        {
            _entryMessages.Clear();
            _exceptionMessages.Clear();
        }
    }
}
