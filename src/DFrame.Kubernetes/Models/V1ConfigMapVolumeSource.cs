using System.Collections.Generic;

namespace DFrame.KubernetesWorker.Models
{
    public class V1ConfigMapVolumeSource
    {
        public int? defaultNode { get; set; }
        public IList<V1KeyToPath> Items { get; set; }
        public string name { get; set; }
    }
}
