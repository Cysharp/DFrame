namespace DFrame.Kubernetes.Models
{
    public class V1EnvVar
    {
        public string Name { get; set; }
        public string Value { get; set; }
        public V1EnvVarSource ValueFrom { get; set; }
    }
}
