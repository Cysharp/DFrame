namespace DFrame.Kubernetes.Models
{
    public class V1PreferredSchedulingTerm
    {
        public V1NodeSelectorTerm Preference { get; set; }
        public int Weight { get; set; }
    }
}
