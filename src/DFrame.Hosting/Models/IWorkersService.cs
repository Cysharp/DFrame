using System;
using System.Threading.Tasks;
using DFrame.Hosting.Data;

namespace DFrame.Hosting.Models
{
    public interface IWorkersService
    {
        /// <summary>
        /// Get statistics
        /// </summary>
        /// <returns></returns>
        Task<WorkerData[]> IniatilizeAsync();
        event Action<WorkerData[]> OnUpdateWorker;
    }

    // todo: prepare WorkerService. Get DFrameWorker Info from Dframe
}
