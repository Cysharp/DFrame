using System;
using System.Collections.Generic;
using System.Text;

namespace DFrame.Kubernetes.Models
{
    public class V1Namespace
    {
        public string ApiVersion { get; set; }
        public string Kind { get; set; }
        public V1ObjectMeta Metadata { get; set; }
        public V1NamespaceSpec Spec { get; set; }
        public V1NamespaceStatus Status { get; set; }
    }
}
