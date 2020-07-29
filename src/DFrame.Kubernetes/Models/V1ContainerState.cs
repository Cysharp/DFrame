namespace DFrame.Kubernetes.Models
{
    public class V1ContainerState
    {
        public V1ContainerStateRunning running { get; set; }
        public V1ContainerStateTerminated terminated { get; set; }
        public V1ContainerStateWaiting waiting { get; set; }
    }
}