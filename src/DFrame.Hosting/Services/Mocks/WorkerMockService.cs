using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DFrame.Hosting.Data;

namespace DFrame.Hosting.Services
{
    /// <summary>
    /// Mock data
    /// </summary>
    public class WorkerMockService : IWorkersService
    {
        public Action<int>? OnWorkerUpdate { get; set; }

        public Task<WorkerData[]> GetWorkers()
        {
            var worker = new WorkerData
            {
                Id = 0,
                Name = Environment.MachineName,
                State = "ready",
                Users = 0,
                Cpu = 10.1d,
            };
            var workers = new[] { worker };

            OnWorkerUpdate?.Invoke(workers.Length);
            return Task.FromResult(workers);
        }

        public Task IniatilizeAsync()
        {
            return Task.CompletedTask;
        }
    }
}
