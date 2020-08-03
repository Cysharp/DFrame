using System.Collections.Generic;

namespace DFrame.Kubernetes.Models
{
    public class V1PodSpec
    {
        public long? ActiveDeadlineSeconds { get; set; }
        public V1Affinity Affinity { get; set; }
        public bool? AutomountServiceAccountToken { get; set; }
        public IList<V1Container> Containers { get; set; }
        public V1PodDNSConfig DnsConfig { get; set; }
        public string DnsPolicy { get; set; }
        public bool? EnableServiceLinks { get; set; }
        public IList<V1EphemeralContainer> EphemeralContainers { get; set; }
        public IList<V1HostAlias> HostAliases { get; set; }
        public bool? HostIpc { get; set; }
        public bool? HostNetwork { get; set; }
        public bool? HostPid { get; set; }
        public string Hostname { get; set; }
        public IList<V1LocalObjectReference> ImagePullSecrets { get; set; }
        public IList<V1Container> InitContainers { get; set; }
        public string NodeName { get; set; }
        public IDictionary<string, string> NodeSelector { get; set; }
        public IDictionary<string, ResourceQuantity> Overhead { get; set; }
        public string PreemptionPolicy { get; set; }
        public int? Priority { get; set; }
        public string PriorityClassName { get; set; }
        public IList<V1PodReadinessGate> ReadinessGates { get; set; }
        public string RestartPolicy { get; set; }
        public string RuntimeClassName { get; set; }
        public string SchedulerName { get; set; }
        public V1PodSecurityContext SecurityContext { get; set; }
        public string ServiceAccount { get; set; }
        public string ServiceAccountName { get; set; }
        public bool? ShareProcessNamespace { get; set; }
        public string Subdomain { get; set; }
        public long? TerminationGracePeriodSeconds { get; set; }
        public IList<V1Toleration> Tolerations { get; set; }
        public IList<V1TopologySpreadConstraint> TopologySpreadConstraints { get; set; }
        public IList<V1Volume> Volumes { get; set; }
    }
}
