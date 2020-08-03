namespace DFrame.Kubernetes.Models
{
    public class V1Lifecycle
    {
        public V1Handler PostStart { get; set; }
        public V1Handler PreStop { get; set; }
    }
}
