using System.Collections.Generic;

namespace DFrame.Kubernetes.Models
{
    public class V1NodeSelectorTerm
    {
        public IList<V1NodeSelectorRequirement> matchExpressions { get; set; }
        public IList<V1NodeSelectorRequirement> matchFields { get; set; }
    }
}
