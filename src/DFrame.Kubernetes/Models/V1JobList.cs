using System;
using System.Collections.Generic;
using System.Text;

namespace DFrame.Kubernetes.Models
{
    public class V1JobList
    {
        public string ApiVersion { get; set; }
        public IList<V1Job> Items { get; set; }
        public string Kind { get; set; }
        public V1ListMeta Metadata { get; set; }
    }
}
