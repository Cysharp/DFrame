using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using DFrame.Kubernetes.Models;

namespace DFrame.Kubernetes.Serializers
{
    /// <summary>
    /// Force handle JSON number as string type.
    /// </summary>
    public class IntOrStringConverter : JsonConverter<string>
    {
        public override string Read(ref Utf8JsonReader reader, Type type, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.Number)
            {
                return reader.TryGetInt64(out long l)
                    ? l.ToString()
                    : reader.GetDouble().ToString();
            }
            if (reader.TokenType == JsonTokenType.String)
            {
                return reader.GetString();
            }

            using JsonDocument document = JsonDocument.ParseValue(ref reader);
            return document.RootElement.Clone().ToString();
        }

        public override void Write(Utf8JsonWriter writer, string value, JsonSerializerOptions options)
        {
            writer.WriteStringValue(value.ToString());
        }
    }

    public class ResourceQuantityConverter : JsonConverter<ResourceQuantity>
    {
        public override ResourceQuantity Read(ref Utf8JsonReader reader, Type type, JsonSerializerOptions options)
        {
            return new ResourceQuantity()
            {
                value = reader.GetString(),
            };
        }

        public override void Write(Utf8JsonWriter writer, ResourceQuantity value, JsonSerializerOptions options)
        {
            if (!string.IsNullOrEmpty(value.value))
            {
                writer.WriteStringValue(value.value.ToString());
            }
        }
    }
}
