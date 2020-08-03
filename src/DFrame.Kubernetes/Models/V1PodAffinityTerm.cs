using System.Collections.Generic;

namespace DFrame.Kubernetes.Models
{
    public class V1PodAffinityTerm
    {
        public V1LabelSelector LabelSelector { get; set; }
        public IList<string> Namespaces { get; set; }
        public string TopologyKey { get; set; }
    }
}
