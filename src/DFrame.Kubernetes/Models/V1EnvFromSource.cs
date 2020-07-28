namespace DFrame.Kubernetes.Models
{
    public class V1EnvFromSource
    {
        public V1ConfigMapEnvSource configMapRef { get; set; }
        public string prefix { get; set; }
        public V1SecretEnvSource secretRef { get; set; }
    }
}
