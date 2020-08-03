namespace DFrame.Kubernetes.Models
{
    public class V1ConfigMapKeySelector
    {
        public string Key { get; set; }
        public string Name { get; set; }
        public bool? Optional { get; set; }
    }
}
