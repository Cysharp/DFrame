namespace DFrame.Kubernetes.Models
{
    public class V1EnvVar
    {
        public string name { get; set; }
        public string value { get; set; }
        public V1EnvVarSource valueFrom { get; set; }
    }
}
