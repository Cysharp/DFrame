namespace DFrame.Kubernetes.Models
{
    public class V1ContainerStatus
    {
        public string ContainerId { get; set; }
        public string Image { get; set; }
        public string ImageId { get; set; }
        public V1ContainerState LastState { get; set; }
        public string Name { get; set; }
        public bool Ready { get; set; }
        public int RestartCount { get; set; }
        public bool? Started { get; set; }
        public V1ContainerState State { get; set; }
    }
}