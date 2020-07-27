namespace DFrame.KubernetesWorker.Models
{
    public class V1SecretEnvSource
    {
        public string name { get; set; }
        public bool? optional { get; set; }
    }
}
