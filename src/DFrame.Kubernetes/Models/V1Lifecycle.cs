namespace DFrame.Kubernetes.Models
{
    public class V1Lifecycle
    {
        public V1Handler postStart { get; set; }
        public V1Handler preStop { get; set; }
    }
}
