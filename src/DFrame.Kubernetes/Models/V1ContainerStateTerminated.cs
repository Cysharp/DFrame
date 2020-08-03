using System;

namespace DFrame.Kubernetes.Models
{
    public class V1ContainerStateTerminated
    {
        public string ContainerId { get; set; }
        public int ExitCode { get; set; }
        public DateTime? FinishedAt { get; set; }
        public string Message { get; set; }
        public string Reason { get; set; }
        public int? Signal { get; set; }
        public DateTime? StartedAt { get; set; }
    }
}