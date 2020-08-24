using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using DFrame.Web.Models;

namespace DFrame.Web
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
        ExecuteArgument ExecuteArgument { get; }

        Task ExecuteAsync();
        Task StopAsync();
    }

    public class ExecuteArgument
    {
        public int ProcessCount { get; set; } = 1;
        public int WorkerPerProcess { get; set; } = 20;
        public int ExecutePerWorker { get; set; } = 500;
        public string WorkerName { get; set; }
        public string[] Arguments { get; set; }
    }

    public class ExecuteContext : IExecuteContext
    {
        public string ExecuteId { get; }
        public string Status { get; private set; }
        public string HostAddress { get; }
        public ExecuteArgument ExecuteArgument { get; }

        public ExecuteContext(string executeId, string hostAddress, ExecuteArgument arguments)
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

        public Task StopAsync()
        {
            Status = "STOP";
            return Task.CompletedTask;
        }
    }
}
