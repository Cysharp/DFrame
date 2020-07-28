using System.Collections.Generic;

namespace DFrame.Kubernetes.Models
{
    public class V1HostAlias
    {
        public IList<string> hostnames { get; set; }
        public string ip { get; set; }
    }
}
