namespace DFrame.Kubernetes.Models
{
    public class V1ContainerPort
    {
        public int ContainerPort { get; set; }
        public string HostIp { get; set; }
        public int HostPort { get; set; }
        public string Name { get; set; }
        public string Protocol { get; set; }
    }
}
