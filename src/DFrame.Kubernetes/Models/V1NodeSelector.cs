using System.Collections.Generic;

namespace DFrame.KubernetesWorker.Models
{
    public class V1NodeSelector
    {
        public IList<V1NodeSelectorTerm> nodeSelectorTerms { get; set; }
    }
}
