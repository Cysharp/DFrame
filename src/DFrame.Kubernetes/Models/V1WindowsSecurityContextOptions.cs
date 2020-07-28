namespace DFrame.Kubernetes.Models
{
    public class V1WindowsSecurityContextOptions
    {
        public string gmsaCredentialSpec { get; set; }
        public string gmsaCredentialSpecName { get; set; }
        public string runAsUserName { get; set; }
    }
}
