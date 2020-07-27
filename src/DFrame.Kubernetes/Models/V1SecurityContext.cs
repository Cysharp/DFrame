namespace DFrame.KubernetesWorker.Models
{
    public class V1SecurityContext
    {
        public bool? allowPrivilegeEscalation { get; set; }
        public V1Capabilities capabilities { get; set; }
        public bool? privileged { get; set; }
        public string procMount { get; set; }
        public bool? readOnlyRootFilesystem { get; set; }
        public long? runAsGroup { get; set; }
        public bool? runAsNonRoot { get; set; }
        public long? runAsUser { get; set; }
        public V1SELinuxOptions seLinuxOptions { get; set; }
        public V1WindowsSecurityContextOptions windowsOptions { get; set; }
    }
}
