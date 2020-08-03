namespace DFrame.Kubernetes.Models
{
    public class V1Status
    {
        public string ApiVersion { get; set; }
        public int? Code { get; set; }
        public V1StatusDetails Details { get; set; }
        public string Kind { get; set; }
        public string Message { get; set; }
        public V1ListMeta Metadata { get; set; }
        public string Reason { get; set; }
        public string Status { get; set; }
    }
}
