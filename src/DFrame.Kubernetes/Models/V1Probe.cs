namespace DFrame.KubernetesWorker.Models
{
    public class V1Probe
    {
        public V1ExecAction exec { get; set; }
        public int? failureThreshold { get; set; }
        public V1HTTPGetAction httpGet { get; set; }
        public int? initialDelaySeconds { get; set; }
        public int? periodSeconds { get; set; }
        public int? successThreshold { get; set; }
        public V1TCPSocketAction tcpSocket { get; set; }
        public int? timeoutSeconds { get; set; }
    }
}
