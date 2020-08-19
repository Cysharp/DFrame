using System;
using System.Threading.Tasks;

namespace DFrame.Web.Data
{
    public interface IWorkersService
    {
        /// <summary>
        /// Get statistics
        /// </summary>
        /// <returns></returns>
        public Task<Worker[]> IniatilizeAsync();
        event Action<Worker[]> OnUpdateWorker;
    }

    /// <summary>
    /// Mock data
    /// </summary>
    public class WorkerMockService : IWorkersService
    {
        public event Action<Worker[]> OnUpdateWorker;

        public Task<Worker[]> IniatilizeAsync()
        {
            var workers = new[]{
                new Worker
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
}
