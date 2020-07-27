namespace DFrame.KubernetesWorker.Models
{
    public class V1ResourceFieldSelector
    {
        public string containerName { get; set; }
        public ResourceQuantity divisor { get; set; }
        public string resource { get; set; }
    }
}
