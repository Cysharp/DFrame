using System.Collections.Generic;

namespace DFrame.Kubernetes.Models
{
    public class V1CsiVolumeSource
    {
        public string Driver { get; set; }
        public string FsType { get; set; }
        public V1LocalObjectReference NodePublishSecretRef { get; set; }
        public bool? ReadOnly { get; set; }
        public IDictionary<string, string> VolumeAttributes { get; set; }
    }
}
