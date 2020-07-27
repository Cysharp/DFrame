using System.Collections.Generic;

namespace DFrame.KubernetesWorker.Models
{
    public class V1FCVolumeSource
    {
        public string fsType { get; set; }
        public int? lun { get; set; }
        public bool? readOnly { get; set; }
        public IList<string> targetWWNs { get; set; }
        public IList<string> wwids { get; set; }
    }
}
