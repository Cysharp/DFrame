namespace DFrame.KubernetesWorker.Models
{
    public class V1TopologySpreadConstraint
    {
        public V1LabelSelector labelSelector { get; set; }
        public int maxSkew { get; set; }
        public string topologyKey { get; set; }
        public string whenUnsatisfiable { get; set; }
    }
}
