using System;
using System.Collections.Generic;
using System.Text;

namespace DFrame.KubernetesWorker.Models
{
    public class V1Namespace
    {
        public string apiVersion { get; set; }
        public string kind { get; set; }
        public V1ObjectMeta metadata { get; set; }
        public V1NamespaceSpec spec { get; set; }
        public V1NamespaceStatus status { get; set; }
    }
}
