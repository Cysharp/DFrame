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
        public Task<Worker[]> GetStatisticsAsync();
    }

    /// <summary>
    /// Mock data
    /// </summary>
    public class WorkerMockService : IWorkersService
    {
        public Task<Worker[]> GetStatisticsAsync()
        {
            return Task.FromResult(new[]
            { 
                new Worker
                {
                    Name = Environment.MachineName,
                    State = "ready",
                    Users = 0,
                    Cpu = 10.1d,
                },
            });
        }
    }
}
