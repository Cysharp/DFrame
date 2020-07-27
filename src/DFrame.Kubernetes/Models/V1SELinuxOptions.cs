namespace DFrame.KubernetesWorker.Models
{
    public class V1SELinuxOptions
    {
        public string level { get; set; }
        public string role { get; set; }
        public string type { get; set; }
        public string user { get; set; }
    }
}
