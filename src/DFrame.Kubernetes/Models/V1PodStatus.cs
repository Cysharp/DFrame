using System;
using System.Collections.Generic;

namespace DFrame.Kubernetes.Models
{
    public class V1PodStatus
    {
        public IList<V1PodCondition> Conditions { get; set; }
        public IList<V1ContainerStatus> ContainerStatuses { get; set; }
        public IList<V1ContainerStatus> EphemeralContainerStatuses { get; set; }
        public string HostIp { get; set; }
        public IList<V1ContainerStatus> InitContainerStatuses { get; set; }
        public string Message { get; set; }
        public string NominatedNodeName { get; set; }
        public string Phase { get; set; }
        public string PodIp { get; set; }
        public IList<V1PodIp> PodIPs { get; set; }
        public string QosClass { get; set; }
        public string Reason { get; set; }
        public DateTime? StartTime { get; set; }
    }
}