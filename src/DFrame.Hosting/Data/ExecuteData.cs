using DFrame.Hosting.Internal;
using System;
using System.ComponentModel.DataAnnotations;

namespace DFrame.Hosting.Data
{
    [Flags]
    public enum ExecuteMode
    {
        [Display(Name = "batch")]
        Batch,
        [Display(Name = "request")]
        Request,
        [Display(Name = "rampup")]
        Rampup,
    }

    public interface IExecuteData
    {
        string[] CreateArguments();
    }

    public class ExecuteData
    {
        /// <summary>
        /// Host Address to load test
        /// </summary>
        [Required(ErrorMessage = "Input HostAddress to load test")]
        public string HostAddress { get; set; } = string.Empty;

        [Required(ErrorMessage = "Select mode")]
        public ExecuteMode Mode { get; set; } = ExecuteMode.Batch;

        [Required(ErrorMessage = "Input workload name.")]
        public string WorkloadName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Input worker count")]
        public int WorkerCount { get; set; } = 1;

        public RequestData Request { get; set; } = new RequestData();
        public RampupData Rampup { get; set; } = new RampupData();

        public string[]? Arguments => CreateArguments();
        public bool IsExecutable => Executable();

        private bool Executable()
        {
            return Mode switch
            {
                ExecuteMode.Batch => !string.IsNullOrWhiteSpace(HostAddress)
                    && !string.IsNullOrWhiteSpace(WorkloadName)
                    && WorkerCount > 0,
                ExecuteMode.Request => !string.IsNullOrWhiteSpace(HostAddress)
                    && !string.IsNullOrWhiteSpace(WorkloadName)
                    && WorkerCount > 0
                    && Request.WorkloadPerWorker > 0
                    && Request.ExecutePerWorkload > 0,
                ExecuteMode.Rampup => !string.IsNullOrWhiteSpace(HostAddress)
                    && !string.IsNullOrWhiteSpace(WorkloadName)
                    && WorkerCount > 0
                    && Rampup.MaxWorkloadPerWorker > 0
                    && Rampup.WorkloadSpawnCount > 0
                    && Rampup.WorkloadSpawnSecond > 0,
                _ => throw new NotImplementedException(),
            };
        }

        private string[] CreateArguments()
        {
            return Mode switch
            {
                ExecuteMode.Batch => $"{Mode.GetDisplayName()} -workloadName {WorkloadName} -workerCount {WorkerCount}".Split(' '),
                ExecuteMode.Request => $"{Mode.GetDisplayName()} -workloadName {WorkloadName} -workerCount {WorkerCount} -workloadPerWorker {Request.WorkloadPerWorker} -executePerWorkload {Request.ExecutePerWorkload}".Split(' '),
                ExecuteMode.Rampup => $"{Mode.GetDisplayName()} -workloadName {WorkloadName} -workerCount {WorkerCount} -maxWorkloadPerWorker {Rampup.MaxWorkloadPerWorker} -workloadSpawnCount {Rampup.WorkloadSpawnCount} -workloadSpawnSecond {Rampup.WorkloadSpawnSecond}".Split(' '),
                _ => throw new NotImplementedException(),
            };
        }

        public class RequestData
        {
            public int WorkloadPerWorker { get; set; } = 1;
            public int ExecutePerWorkload { get; set; } = 1;
        }

        public class RampupData
        {
            public int MaxWorkloadPerWorker { get; set; } = 1;
            public int WorkloadSpawnCount { get; set; } = 1;
            public int WorkloadSpawnSecond { get; set; } = 1;
        }
    }
}
