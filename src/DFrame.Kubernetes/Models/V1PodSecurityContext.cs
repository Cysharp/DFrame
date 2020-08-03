using System.Collections.Generic;

namespace DFrame.Kubernetes.Models
{
    public class V1PodSecurityContext
    {
        public long? FsGroup { get; set; }
        public string FsGroupChangePolicy { get; set; }
        public long? RunAsGroup { get; set; }
        public bool? RunAsNonRoot { get; set; }
        public long? RunAsUser { get; set; }
        public V1SELinuxOptions SeLinuxOptions { get; set; }
        public IList<long?> SupplementalGroups { get; set; }
        public IList<V1Sysctl> Sysctls { get; set; }
        public V1WindowsSecurityContextOptions WindowsOptions { get; set; }
    }
}
