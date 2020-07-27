using System.Text.Json.Serialization;
using DFrame.Kubernetes.Converters;

namespace DFrame.KubernetesWorker.Models
{
    public class V1TCPSocketAction
    {
        public string host { get; set; }
        [JsonConverter(typeof(IntOrStringConverter))]

        public string port { get; set; }
    }
}
