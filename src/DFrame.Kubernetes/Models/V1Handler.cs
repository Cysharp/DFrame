namespace DFrame.Kubernetes.Models
{
    public class V1Handler
    {
        public V1ExecAction Exec { get; set; }
        public V1HTTPGetAction HttpGet { get; set; }
        public V1TCPSocketAction TcpSocket { get; set; }
    }
}
