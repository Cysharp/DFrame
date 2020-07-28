namespace DFrame.Kubernetes.Models
{
    public class V1ListMeta
    {
        public string @continue { get; set; }
        public long? remainingItemCount { get; set; }
        public string resourceVersion { get; set; }
        public string selfLink { get; set; }
    }
}
