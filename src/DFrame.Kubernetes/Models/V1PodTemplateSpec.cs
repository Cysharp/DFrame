namespace DFrame.Kubernetes.Models
{
    public class V1PodTemplateSpec
    {
        public V1ObjectMeta Metadata { get; set; }
        public V1PodSpec Spec { get; set; }
    }
}
