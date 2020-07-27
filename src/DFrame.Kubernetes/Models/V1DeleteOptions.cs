using System.Collections.Generic;

namespace DFrame.KubernetesWorker.Models
{
    public class V1DeleteOptions
    {
        public string apiVersion { get; set; }
        public IList<string> dryRun { get; set; }
        public long? gracePeriodSeconds { get; set; }
        public string kind { get; set; }
        public bool? orphanDependents { get; set; }
        public V1Preconditions preconditions { get; set; }
        public string propagationPolicy { get; set; }
    }
}
