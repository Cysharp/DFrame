using Cysharp.Text;
using DFrame.Hosting.Data;
using DFrame.Hosting.Services;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ZLogger;

namespace DFrame.Hosting.Infrastructure
{
    public class LogProcessorOptions
    {
        public LogLevel LogLevel { get; set; } = LogLevel.Information;
        public ZLoggerOptions LoggerOptions { get; set; } = new ZLoggerOptions();
    }

    public interface IExecuteLogProcessor : IAsyncLogProcessor
    {
        LogLevel LogLevel { get; }

        LogMessage[] GetAll();
        FailureMessage[] GetExceptions();
        LogMessage Remove();
        FailureMessage RemoveException();
        void Clear();
    }

    public class ExecuteLogProcessor : IExecuteLogProcessor
    {
        private readonly LogProcessorOptions options;
        // normal log + exception log
        private readonly ConcurrentQueue<LogMessage> _entryMessages;
        // exception log only
        private readonly ConcurrentQueue<FailureMessage> _exceptionMessages;

        public LogLevel LogLevel => options.LogLevel;

        public ExecuteLogProcessor(LogProcessorOptions options)
        {
            this.options = options;
            _entryMessages = new ConcurrentQueue<LogMessage>();
            _exceptionMessages = new ConcurrentQueue<FailureMessage>();
        }

        public ValueTask DisposeAsync()
        {
            return default;
        }

        public void Post(IZLoggerEntry log)
        {
            _entryMessages.Enqueue(new LogMessage
            {
                TimeStamp = log.LogInfo.Timestamp,
                Message = log.FormatToString(options.LoggerOptions, null),
            });

            if (log.LogInfo.LogLevel >= LogLevel.Error)
            {
                // todo: structured fails
                // fails = group by method and name 
                // method = HTTP Method
                // name = path of benchmark
                var failure = new FailureMessage
                {
                    TimeStamp = log.LogInfo.Timestamp,
                    Method = "TBD",
                    Path = "/PATH/LOAD/TEST",
                    Message = log.FormatToString(options.LoggerOptions, null),
                };
                _exceptionMessages.Enqueue(failure);
            }
        }

        public LogMessage[] GetAll()
        {
            var list = new List<LogMessage>();
            foreach (var message in _entryMessages)
            {
                list.Add(message);
            }
            return list.ToArray();
        }

        public FailureMessage[] GetExceptions()
        {
            var list = new List<FailureMessage>();
            foreach (var message in _exceptionMessages)
            {
                list.Add(message);
            }
            return list.ToArray();
        }

        public LogMessage Remove()
        {
            _entryMessages.TryDequeue(out var ret);
            return ret;
        }

        public FailureMessage RemoveException()
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
