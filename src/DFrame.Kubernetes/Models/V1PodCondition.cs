using System;

namespace DFrame.Kubernetes.Models
{
    public class V1PodCondition
    {
        public DateTime? lastProbeTime { get; set; }
        public DateTime? lastTransitionTime { get; set; }
        public string message { get; set; }
        public string reason { get; set; }
        public string status { get; set; }
        public string type { get; set; }
    }
}