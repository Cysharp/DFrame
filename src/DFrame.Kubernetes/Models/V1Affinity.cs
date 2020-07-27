namespace DFrame.KubernetesWorker.Models
{
    public class V1Affinity
    {
        public V1Nodeaffinity nodeAffinity { get; set; }
        public V1Podaffinity podAffinity { get; set; }
        public V1Podantiaffinity podAntiAffinity { get; set; }
    }
}
