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

        [Required(ErrorMessage = "Input worker name.")]
        public string WorkerName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Input process count")]
        public int ProcessCount { get; set; } = 1;

        public RequestData Request { get; set; } = new RequestData();
        public RampupData Rampup { get; set; } = new RampupData();

        public string[]? Arguments => CreateArguments();
        public bool IsExecutable => Executable();

        private bool Executable()
        {
            return Mode switch
            {
                ExecuteMode.Batch => !string.IsNullOrWhiteSpace(HostAddress)
                    && !string.IsNullOrWhiteSpace(WorkerName)
                    && ProcessCount > 0,
                ExecuteMode.Request => !string.IsNullOrWhiteSpace(HostAddress)
                    && !string.IsNullOrWhiteSpace(WorkerName)
                    && ProcessCount > 0
                    && Request.WorkerPerProcess > 0
                    && Request.ExecutePerWorker > 0,
                ExecuteMode.Rampup => !string.IsNullOrWhiteSpace(HostAddress)
                    && !string.IsNullOrWhiteSpace(WorkerName)
                    && ProcessCount > 0
                    && Rampup.MaxWorkerPerProcess > 0
                    && Rampup.WorkerSpawnCount > 0
                    && Rampup.WorkerSpawnSecond > 0,
                _ => throw new NotImplementedException(),
            };
        }

        private string[] CreateArguments()
        {
            return Mode switch
            {
                ExecuteMode.Batch => $"{Mode.GetDisplayName()} -workerName {WorkerName} -processCount {ProcessCount}".Split(' '),
                ExecuteMode.Request => $"{Mode.GetDisplayName()} -workerName {WorkerName} -processCount {ProcessCount} -workerPerProcess {Request.WorkerPerProcess} -executePerWorker {Request.ExecutePerWorker}".Split(' '),
                ExecuteMode.Rampup => $"{Mode.GetDisplayName()} -workerName {WorkerName} -processCount {ProcessCount} -maxWorkerPerProcess {Rampup.MaxWorkerPerProcess} -workerSpawnCount {Rampup.WorkerSpawnCount} -workerSpawnSecond {Rampup.WorkerSpawnSecond}".Split(' '),
                _ => throw new NotImplementedException(),
            };
        }

        public class RequestData
        {
            public int WorkerPerProcess { get; set; } = 1;
            public int ExecutePerWorker { get; set; } = 1;
        }

        public class RampupData
        {
            public int MaxWorkerPerProcess { get; set; } = 1;
            public int WorkerSpawnCount { get; set; } = 1;
            public int WorkerSpawnSecond { get; set; } = 1;
        }
    }
}
