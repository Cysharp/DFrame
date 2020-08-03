namespace DFrame.Kubernetes.Models
{
    public class V1TopologySpreadConstraint
    {
        public V1LabelSelector LabelSelector { get; set; }
        public int MaxSkew { get; set; }
        public string TopologyKey { get; set; }
        public string WhenUnsatisfiable { get; set; }
    }
}
