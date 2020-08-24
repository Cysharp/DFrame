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
        /// WorkerName to run
        /// </summary>
        string WorkerName { get; }
        string[] Args { get; }
        string Arg { get; }

        Task ExecuteAsync();
        Task StopAsync();
    }

    public class ExecuteContext : IExecuteContext
    {
        public string ExecuteId { get; }
        public string Status { get; private set; }
        public string HostAddress { get; }
        public string WorkerName { get; }
        public string[] Args { get; private set; } = Array.Empty<string>();
        public string Arg { get; }

        public ExecuteContext(string executeId, string hostAddress, string workerName, string arg)
        {
            ExecuteId = executeId;
            HostAddress = hostAddress;
            WorkerName = workerName;
            Arg = arg;
            Args = Arg.Split(' ');
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
