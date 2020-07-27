namespace DFrame.KubernetesWorker.Models
{
    public class V1Handler
    {
        public V1ExecAction exec { get; set; }
        public V1HTTPGetAction httpGet { get; set; }
        public V1TCPSocketAction tcpSocket { get; set; }
    }
}
