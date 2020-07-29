namespace DFrame.Kubernetes.Models
{
    public class V1Pod
    {
        public string apiVersion { get; set; }
        public string kind { get; set; }
        public V1ObjectMeta metadata { get; set; }
        public V1PodSpec spec { get; set; }
        public V1PodStatus status { get; set; }
    }
}