using System.Text.Json.Serialization;
using DFrame.Kubernetes.Converters;

namespace DFrame.KubernetesWorker.Models
{
    [JsonConverter(typeof(ResourceQuantityConverter))]
    public class ResourceQuantity
    {
        public string value { get; set; }
    }
}
