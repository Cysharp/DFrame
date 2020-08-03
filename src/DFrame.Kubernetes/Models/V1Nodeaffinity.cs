using System.Collections.Generic;

namespace DFrame.Kubernetes.Models
{
    public class V1Nodeaffinity
    {
        public IList<V1PreferredSchedulingTerm> PreferredDuringSchedulingIgnoredDuringExecution { get; set; }
        public V1NodeSelector RequiredDuringSchedulingIgnoredDuringExecution { get; set; }
    }
}
