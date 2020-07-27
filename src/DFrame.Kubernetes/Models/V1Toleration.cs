namespace DFrame.KubernetesWorker.Models
{
    public class V1Toleration
    {
        public string effect { get; set; }
        public string key { get; set; }
        public string @operator { get; set; }
        public long? tolerationSeconds { get; set; }
    }
}
