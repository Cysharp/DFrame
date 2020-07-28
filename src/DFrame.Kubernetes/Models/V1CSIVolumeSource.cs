using System.Collections.Generic;

namespace DFrame.Kubernetes.Models
{
    public class V1CSIVolumeSource
    {
        public string driver { get; set; }
        public string fsType { get; set; }
        public V1LocalObjectReference nodePublishSecretRef { get; set; }
        public bool? readOnly { get; set; }
        public IDictionary<string, string> volumeAttributes { get; set; }
    }
}
