using System.Collections.Generic;

namespace DFrame.Kubernetes.Models
{
    public class V1LabelSelectorRequirement
    {
        public string key { get; set; }
        public string @operator { get; set; }
        public IList<string> values { get; set; }
    }
}
