using System.Collections.Generic;

namespace DFrame.Kubernetes.Models
{
    public class V1NodeSelectorTerm
    {
        public IList<V1NodeSelectorRequirement> MatchExpressions { get; set; }
        public IList<V1NodeSelectorRequirement> MatchFields { get; set; }
    }
}
