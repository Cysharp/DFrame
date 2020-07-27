using System;
using System.Collections.Generic;
using System.Text;

namespace DFrame.KubernetesWorker.Models
{
    public class V1NamespaceSpec
    {
        public IList<string> finalizers { get; set; }
    }
}
