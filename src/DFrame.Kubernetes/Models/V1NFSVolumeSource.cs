namespace DFrame.Kubernetes.Models
{
    public class V1NFSVolumeSource
    {
        public string path { get; set; }
        public bool? readOnly { get; set; }
        public string server { get; set; }
    }
}
