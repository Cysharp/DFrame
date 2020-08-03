using System.Collections.Generic;
using System.Text.Json.Serialization;
using DFrame.Kubernetes.Serializers;

namespace DFrame.Kubernetes.Models
{
    public class V1HTTPGetAction
    {
        public string Host { get; set; }
        public IList<V1HTTPHeader> HttpHeaders { get; set; }
        public string Path { get; set; }
        [JsonConverter(typeof(IntOrStringConverter))]
        public string Port { get; set; }
        public string Scheme { get; set; }
    }
}
