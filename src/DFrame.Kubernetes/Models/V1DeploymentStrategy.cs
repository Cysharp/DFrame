namespace DFrame.Kubernetes.Models
{
    public class V1DeploymentStrategy
    {
        public V1RollingUpdateDeployment rollingUpdate { get; set; }
        public string type { get; set; }
    }
}
