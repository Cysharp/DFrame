using System;
using System.Collections.Generic;
using System.Text;

namespace DFrame.Kubernetes.Models
{
    public class V1NamespaceStatus
    {
        public IList<V1NamespaceCondition> Conditions { get; set; }
        public string Phase { get; set; }
    }
}
