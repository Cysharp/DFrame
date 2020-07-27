using System.Collections.Generic;

namespace DFrame.KubernetesWorker.Models
{
    public class V1Capabilities
    {
        public IList<string> add { get; set; }
        public IList<string> drop { get; set; }
    }
}
