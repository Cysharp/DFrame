using System;

namespace DFrame.Kubernetes.Models
{
    public class V1DeploymentCondition
    {
        public DateTime? LastTransitionTime { get; set; }
        public DateTime? LastUpdateTime { get; set; }
        public string Message { get; set; }
        public string Reason { get; set; }
        public string Status { get; set; }
        public string Type { get; set; }
    }
}
