namespace DFrame.Kubernetes.Models
{
    public class V1Job
    {
        public string ApiVersion { get; set; }
        public string Kind { get; set; }
        public V1ObjectMeta Metadata { get; set; }
        public V1JobSpec Spec { get; set; }
        public V1JobStatus Status { get; set; }
    }
}
