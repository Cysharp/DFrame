namespace DFrame.Kubernetes.Models
{
    public class V1Volume
    {
        public V1ConfigMapVolumeSource ConfigMap { get; set; }
        public V1EmptyDirVolumeSource EmptyDir { get; set; }
        public V1FcVolumeSource Fc { get; set; }
        public V1HostPathVolumeSource HostPath { get; set; }
        public string Name { get; set; }
        public V1NFSVolumeSource Nfs { get; set; }
        public V1PersistentVolumeClaimVolumeSource PersistentVolumeClaim { get; set; }
        public V1SecretVolumeSource Secret { get; set; }
    }
}
