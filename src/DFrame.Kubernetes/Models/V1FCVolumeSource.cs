using System.Collections.Generic;

namespace DFrame.Kubernetes.Models
{
    public class V1FcVolumeSource
    {
        public string FsType { get; set; }
        public int? Lun { get; set; }
        public bool? ReadOnly { get; set; }
        public IList<string> TargetWwNs { get; set; }
        public IList<string> Wwids { get; set; }
    }
}
