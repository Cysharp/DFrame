namespace DFrame.Kubernetes.Models
{
    public class V1PodTemplateSpec
    {
        public V1ObjectMeta metadata { get; set; }
        public V1PodSpec spec { get; set; }
    }
}
