namespace DFrame.Kubernetes.Models
{
    public class V1DeploymentSpec
    {
        public int? MinReadySeconds { get; set; }
        public bool? Paused { get; set; }
        public int? ProgressDeadlineSeconds { get; set; }
        public int? Replicas { get; set; }
        public int? RevisionHistoryLimit { get; set; }
        public V1LabelSelector Selector { get; set; }
        public V1DeploymentStrategy Strategy { get; set; }
        public V1PodTemplateSpec Template { get; set; }
    }
}
