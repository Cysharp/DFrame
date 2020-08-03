using System.Text.Json.Serialization;
using DFrame.Kubernetes.Serializers;

namespace DFrame.Kubernetes.Models
{
    [JsonConverter(typeof(ResourceQuantityConverter))]
    public class ResourceQuantity
    {
        public string Value { get; set; }
    }
}
