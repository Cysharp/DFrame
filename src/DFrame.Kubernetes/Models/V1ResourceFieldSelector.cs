namespace DFrame.Kubernetes.Models
{
    public class V1ResourceFieldSelector
    {
        public string ContainerName { get; set; }
        public ResourceQuantity Divisor { get; set; }
        public string Resource { get; set; }
    }
}
