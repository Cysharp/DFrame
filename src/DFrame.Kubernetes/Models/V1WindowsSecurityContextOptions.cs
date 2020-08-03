namespace DFrame.Kubernetes.Models
{
    public class V1WindowsSecurityContextOptions
    {
        public string GmsaCredentialSpec { get; set; }
        public string GmsaCredentialSpecName { get; set; }
        public string RunAsUserName { get; set; }
    }
}
