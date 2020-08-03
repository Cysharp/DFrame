using System.Collections.Generic;

namespace DFrame.Kubernetes.Models
{
    public class V1DeleteOptions
    {
        public string ApiVersion { get; set; }
        public IList<string> DryRun { get; set; }
        public long? GracePeriodSeconds { get; set; }
        public string Kind { get; set; }
        public bool? OrphanDependents { get; set; }
        public V1Preconditions Preconditions { get; set; }
        public string PropagationPolicy { get; set; }
    }
}
