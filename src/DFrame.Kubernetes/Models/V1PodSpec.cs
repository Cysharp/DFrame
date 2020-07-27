using System.Collections.Generic;

namespace DFrame.KubernetesWorker.Models
{
    public class V1PodSpec
    {
        public long? activeDeadlineSeconds { get; set; }
        public V1Affinity affinity { get; set; }
        public bool? automountServiceAccountToken { get; set; }
        public IList<V1Container> containers { get; set; }
        public V1PodDNSConfig dnsConfig { get; set; }
        public string dnsPolicy { get; set; }
        public bool? enableServiceLinks { get; set; }
        public IList<V1EphemeralContainer> ephemeralContainers { get; set; }
        public IList<V1HostAlias> hostAliases { get; set; }
        public bool? hostIPC { get; set; }
        public bool? hostNetwork { get; set; }
        public bool? hostPID { get; set; }
        public string hostname { get; set; }
        public IList<V1LocalObjectReference> imagePullSecrets { get; set; }
        public IList<V1Container> initContainers { get; set; }
        public string nodeName { get; set; }
        public IDictionary<string, string> nodeSelector { get; set; }
        public IDictionary<string, ResourceQuantity> overhead { get; set; }
        public string preemptionPolicy { get; set; }
        public int? priority { get; set; }
        public string priorityClassName { get; set; }
        public IList<V1PodReadinessGate> readinessGates { get; set; }
        public string restartPolicy { get; set; }
        public string runtimeClassName { get; set; }
        public string schedulerName { get; set; }
        public V1PodSecurityContext securityContext { get; set; }
        public string serviceAccount { get; set; }
        public string serviceAccountName { get; set; }
        public bool? shareProcessNamespace { get; set; }
        public string subdomain { get; set; }
        public long? terminationGracePeriodSeconds { get; set; }
        public IList<V1Toleration> tolerations { get; set; }
        public IList<V1TopologySpreadConstraint> topologySpreadConstraints { get; set; }
        public IList<V1Volume> volumes { get; set; }
    }
}
