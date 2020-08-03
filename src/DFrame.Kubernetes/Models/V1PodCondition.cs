using System;

namespace DFrame.Kubernetes.Models
{
    public class V1PodCondition
    {
        public DateTime? LastProbeTime { get; set; }
        public DateTime? LastTransitionTime { get; set; }
        public string Message { get; set; }
        public string Reason { get; set; }
        public string Status { get; set; }
        public string Type { get; set; }
    }
}