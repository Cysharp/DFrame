namespace DFrame.Kubernetes.Models
{
    public class V1NFSVolumeSource
    {
        public string Path { get; set; }
        public bool? ReadOnly { get; set; }
        public string Server { get; set; }
    }
}
