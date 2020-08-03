namespace DFrame.Kubernetes.Models
{
    public class V1SecretKeySelector
    {
        public string Key { get; set; }
        public string Name { get; set; }
        public bool? Optional { get; set; }
    }
}
