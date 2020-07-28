namespace DFrame.Kubernetes.Models
{
    public class V1Ownerreference
    {
        public string apiVersion { get; set; }
        public bool? blockOwnerDeletion { get; set; }
        public bool? controller { get; set; }
        public string kind { get; set; }
        public string name { get; set; }
        public string uid { get; set; }
    }
}
