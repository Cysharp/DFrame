using System.Collections.Generic;

namespace DFrame.Kubernetes.Models
{
    public class V1Capabilities
    {
        public IList<string> Add { get; set; }
        public IList<string> Drop { get; set; }
    }
}
