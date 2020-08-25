using System;
using System.Threading.Tasks;
using DFrame.Hosting.Data;

namespace DFrame.Hosting.Models
{
    /// <summary>
    /// Mock data
    /// </summary>
    public class WorkerMockService : IWorkersService
    {
        public event Action<WorkerData[]> OnUpdateWorker;

        public Task<WorkerData[]> IniatilizeAsync()
        {
            var workers = new[]{
                new WorkerData
                {
                    Name = Environment.MachineName,
                    State = "ready",
                    Users = 0,
                    Cpu = 10.1d,
                },
            };

            OnUpdateWorker?.Invoke(workers);

            return Task.FromResult(workers);
        }
    }

    // todo: prepare WorkerService. Get DFrameWorker Info from Dframe
}
