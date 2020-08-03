using System;

namespace DFrame.Kubernetes.Models
{
    public class V1NamespaceCondition
    {
        public DateTime? LastTransitionTime { get; set; }
        public string Message { get; set; }
        public string Reason { get; set; }
        public string Status { get; set; }
        public string Type { get; set; }
    }
}