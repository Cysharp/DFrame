using System.Collections.Generic;

namespace DFrame.Kubernetes.Models
{
    public class V1SecretVolumeSource
    {
        public int? DefaultMode { get; set; }
        public IList<V1KeyToPath> Items { get; set; }
        public bool? Optional { get; set; }
        public string SecretName { get; set; }
    }
}
