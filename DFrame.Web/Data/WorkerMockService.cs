using System;
using System.Threading.Tasks;

namespace DFrame.Web.Data
{
    public interface IWorkersService
    {
        /// <summary>
        /// Cache
        /// </summary>
        Worker[] Cache { get; }

        /// <summary>
        /// Get statistics
        /// </summary>
        /// <returns></returns>
        public Task<Worker[]> GetStatisticsAsync();
    }

    /// <summary>
    /// Mock data
    /// </summary>
    public class WorkerMockService : IWorkersService
    {
        public Worker[] Cache { get; private set; } = Array.Empty<Worker>();

        public Task<Worker[]> GetStatisticsAsync()
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
            Cache = workers;

            return Task.FromResult(workers);
        }
    }
}
