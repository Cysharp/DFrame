using System.Collections.Generic;

namespace DFrame.Kubernetes.Models
{
    public class V1PodSecurityContext
    {
        public long? fsGroup { get; set; }
        public string fsGroupChangePolicy { get; set; }
        public long? runAsGroup { get; set; }
        public bool? runAsNonRoot { get; set; }
        public long? runAsUser { get; set; }
        public V1SELinuxOptions seLinuxOptions { get; set; }
        public IList<long?> supplementalGroups { get; set; }
        public IList<V1Sysctl> sysctls { get; set; }
        public V1WindowsSecurityContextOptions windowsOptions { get; set; }
    }
}
