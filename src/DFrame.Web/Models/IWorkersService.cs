using System;
using System.Threading.Tasks;
using DFrame.Web.Data;

namespace DFrame.Web.Models
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
