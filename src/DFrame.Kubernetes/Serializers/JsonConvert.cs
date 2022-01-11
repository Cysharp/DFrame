using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace DFrame.Kubernetes.Serializers
{
    public static class JsonConvert
    {
        private static readonly JsonSerializerOptions defaultJsonSerializerOptions;
        private static readonly JsonSerializerOptions enumJsonSerializerOptions;

        static JsonConvert()
        {
            defaultJsonSerializerOptions = new JsonSerializerOptions()
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            };
            enumJsonSerializerOptions = new JsonSerializerOptions()
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            };
            enumJsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
        }

        #region deserialize
        /// <summary>
        /// Deserialize Json to TValue
        /// </summary>
        /// <typeparam name="TValue"></typeparam>
        /// <param name="utf8Json"></param>
        /// <returns></returns>
        public static TValue Deserialize<TValue>(ReadOnlySpan<byte> utf8Json, JsonSerializerOptions options = null)
        {
            options ??= defaultJsonSerializerOptions;
            return JsonSerializer.Deserialize<TValue>(utf8Json, options);
        }
        /// <summary>
        /// Deserialize Json to TValue
        /// </summary>
        /// <typeparam name="TValue"></typeparam>
        /// <param name="json"></param>
        /// <returns></returns>
        public static TValue Deserialize<TValue>(string json, JsonSerializerOptions options = null)
        {
            options ??= defaultJsonSerializerOptions;
            return JsonSerializer.Deserialize<TValue>(json, options);
        }
        /// <summary>
        /// Deserialize Json to TValue
        /// </summary>
        /// <typeparam name="TValue"></typeparam>
        /// <param name="reader"></param>
        /// <returns></returns>
        public static TValue Deserialize<TValue>(ref Utf8JsonReader reader, JsonSerializerOptions options = null)
        {
            options ??= defaultJsonSerializerOptions;
            return JsonSerializer.Deserialize<TValue>(ref reader, options);
        }

        /// <summary>
        /// Deserialize Json to TValue, enum in TValue will convert as string
        /// </summary>
        /// <typeparam name="TValue"></typeparam>
        /// <param name="utf8Json"></param>
        /// <returns></returns>
        public static TValue DeserializeStringEnum<TValue>(ReadOnlySpan<byte> utf8Json)
        {
            return Deserialize<TValue>(utf8Json, enumJsonSerializerOptions);
        }
        /// <summary>
        /// Deserialize Json to TValue, enum in TValue will convert as string
        /// </summary>
        /// <typeparam name="TValue"></typeparam>
        /// <param name="json"></param>
        /// <returns></returns>
        public static TValue DeserializeStringEnum<TValue>(string json)
        {
            return Deserialize<TValue>(json, enumJsonSerializerOptions);
        }
        /// <summary>
        /// Deserialize Json to TValue, enum in TValue will convert as string
        /// </summary>
        /// <typeparam name="TValue"></typeparam>
        /// <param name="reader"></param>
        /// <returns></returns>
        public static TValue DeserializeStringEnum<TValue>(ref Utf8JsonReader reader)
        {
            return Deserialize<TValue>(ref reader, enumJsonSerializerOptions);
        }
        #endregion

        #region serialize
        /// <summary>
        /// Serialize TValue to Json
        /// </summary>
        /// <typeparam name="TValue"></typeparam>
        /// <param name="value"></param>
        /// <returns></returns>
        public static string Serialize<TValue>(TValue value, JsonSerializerOptions options = null)
        {
            options ??= defaultJsonSerializerOptions;
            return JsonSerializer.Serialize<TValue>(value, options);
        }
        #endregion    
    }    
}
