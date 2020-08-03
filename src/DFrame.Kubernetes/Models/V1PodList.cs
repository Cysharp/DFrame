using System.Collections.Generic;

namespace DFrame.Kubernetes.Models
{
    public class V1PodList
    {
        public string ApiVersion { get; set; }
        public IList<V1Pod> Items { get; set; }
        public string Kind { get; set; }
        public V1ListMeta Metadata { get; set; }
    }
}
