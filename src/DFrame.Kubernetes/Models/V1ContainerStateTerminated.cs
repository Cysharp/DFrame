using System;

namespace DFrame.Kubernetes.Models
{
    public class V1ContainerStateTerminated
    {
        public string containerID { get; set; }
        public int exitCode { get; set; }
        public DateTime? finishedAt { get; set; }
        public string message { get; set; }
        public string reason { get; set; }
        public int? signal { get; set; }
        public DateTime? startedAt { get; set; }
    }
}