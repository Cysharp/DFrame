using DFrame.Hosting.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
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
        /// Host Address to load test
        /// </summary>
        string HostAddress { get; }
        /// <summary>
        /// Execute Arguments
        /// </summary>
        ExecuteData ExecuteArgument { get; }

        Task ExecuteAsync();
        Task ErrorAsync();
        Task StopAsync();
    }

    public class ExecuteContext : IExecuteContext
    {
        public string ExecuteId { get; }
        public string Status { get; private set; }
        public string HostAddress { get; }
        public ExecuteData ExecuteArgument { get; }

        public ExecuteContext(string executeId, string hostAddress, ExecuteData arguments)
        {
            ExecuteId = executeId;
            HostAddress = hostAddress;
            ExecuteArgument = arguments;
            Environment.SetEnvironmentVariable("DFRAME_MASTER_HOST", hostAddress, EnvironmentVariableTarget.Process);
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
