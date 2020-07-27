using System.Collections.Generic;

namespace DFrame.KubernetesWorker.Models
{
    public class V1PodDNSConfig
    {
        public IList<string> nameservers { get; set; }
        public IList<V1PodDNSConfigOption> options { get; set; }
        public IList<string> searches { get; set; }
    }
}
