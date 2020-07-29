using System;
using System.Collections.Generic;

namespace DFrame.Kubernetes.Models
{
    public class V1PodStatus
    {
        public IList<V1PodCondition> conditions { get; set; }
        public IList<V1ContainerStatus> containerStatuses { get; set; }
        public IList<V1ContainerStatus> ephemeralContainerStatuses { get; set; }
        public string hostIP { get; set; }
        public IList<V1ContainerStatus> initContainerStatuses { get; set; }
        public string message { get; set; }
        public string nominatedNodeName { get; set; }
        public string phase { get; set; }
        public string podIP { get; set; }
        public IList<V1PodIP> podIPs { get; set; }
        public string qosClass { get; set; }
        public string reason { get; set; }
        public DateTime? startTime { get; set; }
    }
}