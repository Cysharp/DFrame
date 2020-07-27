using System.Collections.Generic;

namespace DFrame.KubernetesWorker.Models
{
    public class V1SecretVolumeSource
    {
        public int? defaultMode { get; set; }
        public IList<V1KeyToPath> Items { get; set; }
        public bool? optional { get; set; }
        public string secretName { get; set; }
    }
}
