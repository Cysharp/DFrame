using System.Collections.Generic;

namespace DFrame.Kubernetes.Models
{
    public class V1DeploymentStatus
    {
        public int? availableReplicas { get; set; }
        public int? collisionCount { get; set; }
        public IList<V1DeploymentCondition> conditions { get; set; }
        public long? observedGeneration { get; set; }
        public int? readyReplicas { get; set; }
        public int? replicas { get; set; }
        public int? unavailableReplicas { get; set; }
        public int? updatedReplicas { get; set; }
    }
}
