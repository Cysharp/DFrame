namespace DFrame.Kubernetes.Models
{
    public class V1Deployment
    {
        public string ApiVersion { get; set; }
        public string Kind { get; set; }
        public V1ObjectMeta Metadata { get; set; }
        public V1DeploymentSpec Spec { get; set; }
        public V1DeploymentStatus Status { get; set; }
    }
}
