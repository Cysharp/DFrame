namespace DFrame.Kubernetes.Models
{
    public class V1ContainerStatus
    {
        public string containerID { get; set; }
        public string image { get; set; }
        public string imageID { get; set; }
        public V1ContainerState lastState { get; set; }
        public string name { get; set; }
        public bool ready { get; set; }
        public int restartCount { get; set; }
        public bool? started { get; set; }
        public V1ContainerState state { get; set; }
    }
}