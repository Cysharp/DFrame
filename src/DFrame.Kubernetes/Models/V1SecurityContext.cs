namespace DFrame.Kubernetes.Models
{
    public class V1SecurityContext
    {
        public bool? AllowPrivilegeEscalation { get; set; }
        public V1Capabilities Capabilities { get; set; }
        public bool? Privileged { get; set; }
        public string ProcMount { get; set; }
        public bool? ReadOnlyRootFilesystem { get; set; }
        public long? RunAsGroup { get; set; }
        public bool? RunAsNonRoot { get; set; }
        public long? RunAsUser { get; set; }
        public V1SELinuxOptions SeLinuxOptions { get; set; }
        public V1WindowsSecurityContextOptions WindowsOptions { get; set; }
    }
}
