namespace DFrame.KubernetesWorker.Models
{
    public class V1ConfigMapKeySelector
    {
        public string key { get; set; }
        public string name { get; set; }
        public bool? optional { get; set; }
    }
}
