namespace DFrame.Kubernetes.Models
{
    public class V1PersistentVolumeClaimVolumeSource
    {
        public string ClaimName { get; set; }
        public bool? ReadOnly { get; set; }
    }
}
