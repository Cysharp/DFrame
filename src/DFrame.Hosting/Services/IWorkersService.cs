using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DFrame.Hosting.Data;

namespace DFrame.Hosting.Services
{
    public interface IWorkersService
    {
        /// <summary>
        /// Get statistics
        /// </summary>
        /// <returns></returns>
        Task IniatilizeAsync();
        Task<WorkerData[]> GetWorkers();

        Action<int>? OnWorkerUpdate { get; set; }
    }

    // todo: prepare WorkersService. Get DFrameWorker Info from Dframe
    public class WorkersService : IWorkersService
    {
        public Action<int>? OnWorkerUpdate { get; set; }

        // ConcurentBag/Queue...?
        private readonly List<WorkerData> _workers = new List<WorkerData>();
        private readonly object _lock = new object();

        public WorkersService()
        {
            DFrame.WorkerProgressNotifier.OnConnected.OnPublished += WorkerConnect;
            DFrame.WorkerProgressNotifier.OnTeardown.OnPublished += WorkerTearDown;
        }

        public Task IniatilizeAsync()
        {
            return Task.CompletedTask;
        }

        public Task<WorkerData[]> GetWorkers()
        {
            return Task.FromResult(_workers.ToArray());
        }

        private void WorkerConnect(int count)
        {
            lock (_lock)
            {
                var worker = new WorkerData
                {
                    Id = count,
                    Name = "", // todo: pass worker machine name
                    State = "RUNNING",
                    Cpu = 0, // todo: calculate worker cpu
                    Users = 0, // todo: WorkerPerProcess User count
                };
                _workers.Add(worker);

                OnWorkerUpdate?.Invoke(_workers.Count);
            }
        }

        private void WorkerTearDown(int count)
        {
            lock (_lock)
            {
                var worker = _workers.FirstOrDefault(x => x.Id == count);
                _workers.Remove(worker);

                OnWorkerUpdate?.Invoke(_workers.Count);
            }
        }
    }
}
