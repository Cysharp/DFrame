using DFrame.Hosting.Data;
using System;
using System.Threading.Tasks;

namespace DFrame.Hosting
{
    public interface IExecuteContext
    {
        /// <summary>
        /// ExecuteId of this session
        /// </summary>
        string ExecuteId { get; }
        /// <summary>
        /// Execute Status
        /// </summary>
        string Status { get; }
        /// <summary>
        /// Execute Arguments
        /// </summary>
        ExecuteData Argument { get; }

        Task ExecuteAsync();
        Task ErrorAsync();
        Task StopAsync();
    }

    public class ExecuteContext : IExecuteContext
    {
        public string ExecuteId { get; }
        public string Status { get; private set; }
        public ExecuteData Argument { get; }

        public ExecuteContext(string executeId, ExecuteData arguments)
        {
            Status = "NOT READY";
            ExecuteId = executeId;
            Argument = arguments;
            Environment.SetEnvironmentVariable("DFRAME_MASTER_HOST", arguments.HostAddress, EnvironmentVariableTarget.Process);
        }

        public Task ExecuteAsync()
        {
            Status = "RUNNING";
            return Task.CompletedTask;
        }

        public Task ErrorAsync()
        {
            Status = "ERROR";
            return Task.CompletedTask;
        }

        public Task StopAsync()
        {
            Status = "STOP";
            return Task.CompletedTask;
        }
    }
}
