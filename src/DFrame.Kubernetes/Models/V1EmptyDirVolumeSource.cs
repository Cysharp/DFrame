namespace DFrame.Kubernetes.Models
{
    public class V1EmptyDirVolumeSource
    {
        public string medium { get; set; }
        public ResourceQuantity sizeLimit { get; set; }
    }
}
