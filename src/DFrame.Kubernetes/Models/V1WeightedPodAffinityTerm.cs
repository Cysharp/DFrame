namespace DFrame.Kubernetes.Models
{
    public class V1WeightedPodAffinityTerm
    {
        public V1PodAffinityTerm PodAffinityTerm { get; set; }
        public int Weight { get; set; }
    }
}
