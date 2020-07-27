using System;
using System.Collections.Generic;

namespace DFrame.KubernetesWorker.Models
{
    public class V1ObjectMeta
    {
        public IDictionary<string, string> annotations { get; set; }
        public string clusterName { get; set; }
        public DateTime? creationTimestamp { get; set; }
        public long? deletionGracePeriodSeconds { get; set; }
        public DateTime? deletionTimestamp { get; set; }
        public IList<string> finalizers { get; set; }
        public string generateName { get; set; }
        public long? generation { get; set; }
        public IDictionary<string, string> labels { get; set; }
        public IList<V1ManagedFieldsEntry> managedFields { get; set; }
        public string name { get; set; }
        public string @namespace { get; set; }
        public IList<V1Ownerreference> ownerReferences { get; set; }
        public string resourceVersion { get; set; }
        public string selfLink { get; set; }
        public string uid { get; set; }
    }
}
