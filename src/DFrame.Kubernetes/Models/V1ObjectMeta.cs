using System;
using System.Collections.Generic;

namespace DFrame.Kubernetes.Models
{
    public class V1ObjectMeta
    {
        public IDictionary<string, string> Annotations { get; set; }
        public string ClusterName { get; set; }
        public DateTime? CreationTimestamp { get; set; }
        public long? DeletionGracePeriodSeconds { get; set; }
        public DateTime? DeletionTimestamp { get; set; }
        public IList<string> Finalizers { get; set; }
        public string GenerateName { get; set; }
        public long? Generation { get; set; }
        public IDictionary<string, string> Labels { get; set; }
        public IList<V1ManagedFieldsEntry> ManagedFields { get; set; }
        public string Name { get; set; }
        public string Namespace { get; set; }
        public IList<V1Ownerreference> OwnerReferences { get; set; }
        public string ResourceVersion { get; set; }
        public string SelfLink { get; set; }
        public string Uid { get; set; }
    }
}
