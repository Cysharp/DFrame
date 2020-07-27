namespace DFrame.KubernetesWorker.Models
{
    public class V1PreferredSchedulingTerm
    {
        public V1NodeSelectorTerm preference { get; set; }
        public int weight { get; set; }
    }
}
