namespace DFrame.Kubernetes.Models
{
    public class V1ContainerState
    {
        public V1ContainerStateRunning Running { get; set; }
        public V1ContainerStateTerminated Terminated { get; set; }
        public V1ContainerStateWaiting Waiting { get; set; }
    }
}