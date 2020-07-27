namespace DFrame.KubernetesWorker.Models
{
    public class V1ContainerPort
    {
        public int containerPort { get; set; }
        public string hostIP { get; set; }
        public int hostPort { get; set; }
        public string name { get; set; }
        public string protocol { get; set; }
    }
}
