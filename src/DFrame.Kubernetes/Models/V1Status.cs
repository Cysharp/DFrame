namespace DFrame.Kubernetes.Models
{
    public class V1Status
    {
        public string apiVersion { get; set; }
        public int? code { get; set; }
        public V1StatusDetails details { get; set; }
        public string kind { get; set; }
        public string message { get; set; }
        public V1ListMeta metadata { get; set; }
        public string reason { get; set; }
        public string status { get; set; }
    }
}
