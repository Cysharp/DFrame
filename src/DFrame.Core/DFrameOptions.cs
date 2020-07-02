using System;

namespace DFrame.Core
{
    public class DFrameOptions
    {
        public Range PortRange { get; }
        public IWorkerScaler WorkerScaler { get; }
        public WorkerScalerOptions WorkerScalerOptions { get; }

        public DFrameOptions(Range portRange, IWorkerScaler workerScaler)
        {
            PortRange = portRange;
            WorkerScaler = workerScaler;
            // TODO:configuration?
            WorkerScalerOptions = new WorkerScalerOptions();
        }
    }
}