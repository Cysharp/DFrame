namespace DFrame.Kubernetes.Models
{
    public class V1EmptyDirVolumeSource
    {
        public string Medium { get; set; }
        public ResourceQuantity SizeLimit { get; set; }
    }
}
