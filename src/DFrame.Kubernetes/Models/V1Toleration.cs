namespace DFrame.Kubernetes.Models
{
    public class V1Toleration
    {
        public string Effect { get; set; }
        public string Key { get; set; }
        public string Operator { get; set; }
        public long? TolerationSeconds { get; set; }
    }
}
