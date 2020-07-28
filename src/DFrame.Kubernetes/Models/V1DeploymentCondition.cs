using System;

namespace DFrame.Kubernetes.Models
{
    public class V1DeploymentCondition
    {
        public DateTime? lastTransitionTime { get; set; }
        public DateTime? lastUpdateTime { get; set; }
        public string message { get; set; }
        public string reason { get; set; }
        public string status { get; set; }
        public string type { get; set; }
    }
}
