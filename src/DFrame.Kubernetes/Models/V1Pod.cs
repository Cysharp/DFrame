namespace DFrame.Kubernetes.Models
{
    public class V1Pod
    {
        public string ApiVersion { get; set; }
        public string Kind { get; set; }
        public V1ObjectMeta Metadata { get; set; }
        public V1PodSpec Spec { get; set; }
        public V1PodStatus Status { get; set; }
    }
}