using System;

namespace DFrame.Kubernetes.Models
{
    public class V1ManagedFieldsEntry
    {
        public string ApiVersion { get; set; }
        public string FieldsType { get; set; }
        public object FieldsV1 { get; set; }
        public string Manager { get; set; }
        public string Operation { get; set; }
        public DateTime? Time { get; set; }
    }
}
