namespace DFrame.Kubernetes.Models
{
    public class V1SecretKeySelector
    {
        public string key { get; set; }
        public string name { get; set; }
        public bool? optional { get; set; }
    }
}
