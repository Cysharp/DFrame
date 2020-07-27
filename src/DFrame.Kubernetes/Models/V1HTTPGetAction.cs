using System.Collections.Generic;
using System.Text.Json.Serialization;
using DFrame.Kubernetes.Converters;

namespace DFrame.KubernetesWorker.Models
{
    public class V1HTTPGetAction
    {
        public string host { get; set; }
        public IList<V1HTTPHeader> httpHeaders { get; set; }
        public string path { get; set; }
        [JsonConverter(typeof(IntOrStringConverter))]
        public string port { get; set; }
        public string scheme { get; set; }
    }
}
