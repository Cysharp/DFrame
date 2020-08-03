namespace DFrame.Kubernetes.Models
{
    public class V1EnvFromSource
    {
        public V1ConfigMapEnvSource ConfigMapRef { get; set; }
        public string Prefix { get; set; }
        public V1SecretEnvSource SecretRef { get; set; }
    }
}
