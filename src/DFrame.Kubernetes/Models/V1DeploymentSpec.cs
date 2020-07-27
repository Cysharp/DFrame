namespace DFrame.KubernetesWorker.Models
{
    public class V1DeploymentSpec
    {
        public int? minReadySeconds { get; set; }
        public bool? paused { get; set; }
        public int? progressDeadlineSeconds { get; set; }
        public int? replicas { get; set; }
        public int? revisionHistoryLimit { get; set; }
        public V1LabelSelector selector { get; set; }
        public V1DeploymentStrategy strategy { get; set; }
        public V1PodTemplateSpec template { get; set; }
    }
}
