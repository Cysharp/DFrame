namespace DFrame.Kubernetes.Models
{
    public class V1Ownerreference
    {
        public string ApiVersion { get; set; }
        public bool? BlockOwnerDeletion { get; set; }
        public bool? Controller { get; set; }
        public string Kind { get; set; }
        public string Name { get; set; }
        public string Uid { get; set; }
    }
}
