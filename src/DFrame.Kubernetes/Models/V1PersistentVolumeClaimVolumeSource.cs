namespace DFrame.Kubernetes.Models
{
    public class V1PersistentVolumeClaimVolumeSource
    {
        public string claimName { get; set; }
        public bool? readOnly { get; set; }
    }
}
