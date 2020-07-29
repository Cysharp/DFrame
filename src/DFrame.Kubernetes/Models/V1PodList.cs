using System;
using System.Collections.Generic;
using System.Text;

namespace DFrame.Kubernetes.Models
{
    public class V1PodList
    {
        public string apiVersion { get; set; }
        public IList<V1Pod> items { get; set; }
        public string kind { get; set; }
        public V1ListMeta metadata { get; set; }
    }
}
