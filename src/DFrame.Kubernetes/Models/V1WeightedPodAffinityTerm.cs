namespace DFrame.Kubernetes.Models
{
    public class V1WeightedPodAffinityTerm
    {
        public V1PodAffinityTerm podAffinityTerm { get; set; }
        public int weight { get; set; }
    }
}
