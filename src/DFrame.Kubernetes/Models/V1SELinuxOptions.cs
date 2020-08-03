namespace DFrame.Kubernetes.Models
{
    public class V1SELinuxOptions
    {
        public string Level { get; set; }
        public string Role { get; set; }
        public string Type { get; set; }
        public string User { get; set; }
    }
}
