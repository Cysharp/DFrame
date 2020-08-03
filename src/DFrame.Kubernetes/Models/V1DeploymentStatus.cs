using System.Collections.Generic;

namespace DFrame.Kubernetes.Models
{
    public class V1DeploymentStatus
    {
        public int? AvailableReplicas { get; set; }
        public int? CollisionCount { get; set; }
        public IList<V1DeploymentCondition> Conditions { get; set; }
        public long? ObservedGeneration { get; set; }
        public int? ReadyReplicas { get; set; }
        public int? Replicas { get; set; }
        public int? UnavailableReplicas { get; set; }
        public int? UpdatedReplicas { get; set; }
    }
}
