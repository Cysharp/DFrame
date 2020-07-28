namespace DFrame.Kubernetes.Models
{
    public class V1Job
    {
        public string apiVersion { get; set; }
        public string kind { get; set; }
        public V1ObjectMeta metadata { get; set; }
        public V1JobSpec spec { get; set; }
        public V1JobStatus status { get; set; }
    }
}
