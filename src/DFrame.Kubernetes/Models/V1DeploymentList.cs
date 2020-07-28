using System.Text;

namespace DFrame.Kubernetes.Models
{
    public class V1DeploymentList
    {
        public string apiVersion { get; set; }
        public V1Deployment[] items { get; set; }
        public string kind { get; set; }
        public V1ListMeta metadata { get; set; }
    }
}
