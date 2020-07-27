namespace DFrame.KubernetesWorker.Models
{
    public class V1Volume
    {
        public V1ConfigMapVolumeSource configMap { get; set; }
        public V1EmptyDirVolumeSource emptyDir { get; set; }
        public V1FCVolumeSource fc { get; set; }
        public V1HostPathVolumeSource hostPath { get; set; }
        public string name { get; set; }
        public V1NFSVolumeSource nfs { get; set; }
        public V1PersistentVolumeClaimVolumeSource persistentVolumeClaim { get; set; }
        public V1SecretVolumeSource secret { get; set; }
    }
}
