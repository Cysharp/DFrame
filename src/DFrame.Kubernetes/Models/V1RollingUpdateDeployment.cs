using System.Text.Json.Serialization;
using DFrame.Kubernetes.Serializers;

namespace DFrame.Kubernetes.Models
{
    public class V1RollingUpdateDeployment
    {
        [JsonConverter(typeof(IntOrStringConverter))]

        public string maxSurge { get; set; }
        [JsonConverter(typeof(IntOrStringConverter))]

        public string maxUnavailable { get; set; }
    }
}
