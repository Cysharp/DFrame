namespace DFrame.Kubernetes.Models
{
    public class V1Probe
    {
        public V1ExecAction Exec { get; set; }
        public int? FailureThreshold { get; set; }
        public V1HTTPGetAction HttpGet { get; set; }
        public int? InitialDelaySeconds { get; set; }
        public int? PeriodSeconds { get; set; }
        public int? SuccessThreshold { get; set; }
        public V1TCPSocketAction TcpSocket { get; set; }
        public int? TimeoutSeconds { get; set; }
    }
}
