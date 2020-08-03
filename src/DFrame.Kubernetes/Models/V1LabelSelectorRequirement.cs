using System.Collections.Generic;

namespace DFrame.Kubernetes.Models
{
    public class V1LabelSelectorRequirement
    {
        public string Key { get; set; }
        public string Operator { get; set; }
        public IList<string> Values { get; set; }
    }
}
