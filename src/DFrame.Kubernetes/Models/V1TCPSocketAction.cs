using System.Text.Json.Serialization;
using DFrame.Kubernetes.Serializers;

namespace DFrame.Kubernetes.Models
{
    public class V1TCPSocketAction
    {
        public string Host { get; set; }
        [JsonConverter(typeof(IntOrStringConverter))]

        public string Port { get; set; }
    }
}
