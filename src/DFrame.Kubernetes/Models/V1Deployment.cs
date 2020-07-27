namespace DFrame.KubernetesWorker.Models
{
    public class V1Deployment
    {
        public string apiVersion { get; set; }
        public string kind { get; set; }
        public V1ObjectMeta metadata { get; set; }
        public V1DeploymentSpec spec { get; set; }
        public V1DeploymentStatus status { get; set; }
    }
}
