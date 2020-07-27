using System;
using System.Buffers;
using System.Buffers.Text;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace DFrame.KubernetesWorker
{
    public class KubernetesApiConfig
    {
        public bool SkipCertificateValidation { get; set; }
        public HeaderContentType ResponseHeaderType { get; set; } = HeaderContentType.Json;

        public void Configure(IKubernetesClient _provider)
        {
            _provider.SkipCertificationValidation = SkipCertificateValidation;
        }
    }

    public partial class Kubernetes
    {
        public bool IsRunningOnKubernetes { get; }
        public string Namespace => _provider.Namespace;

        private IKubernetesClient _provider;
        private KubernetesApiConfig _config = new KubernetesApiConfig();

        public Kubernetes()
        {
            _provider = GetDefaultProvider();
            SetProviderConfig();
            IsRunningOnKubernetes = _provider.IsRunningOnKubernetes;
        }
        public Kubernetes(KubernetesApiConfig config)
        {
            _config = config;
            _provider = GetDefaultProvider();
            SetProviderConfig();

            IsRunningOnKubernetes = _provider.IsRunningOnKubernetes;
        }

        #region API
        private void ConfigureClient(bool skipCertficateValidate)
        {
            _config.SkipCertificateValidation = skipCertficateValidate;
            SetProviderConfig();
        }

        /// <summary>
        /// Get resource
        /// </summary>
        /// <param name="apiPath"></param>
        /// <param name="acceptHeader"></param>
        /// <returns></returns>
        private async ValueTask<string> GetApiAsync(string apiPath, string acceptHeader = default)
        {
            using (var httpClient = _provider.CreateHttpClient())
            {
                SetAcceptHeader(httpClient, acceptHeader);
                var res = await httpClient.GetStringAsync(_provider.KubernetesServiceEndPoint + apiPath);
                return res;
            }
        }

        /// <summary>
        /// Create Resource
        /// </summary>
        /// <param name="apiPath"></param>
        /// <param name="body"></param>
        /// <param name="bodyContenType"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        private async ValueTask<string> PostApiAsync(string apiPath, string body, string bodyContenType = "application/yaml", CancellationToken ct = default)
        {
            using (var httpClient = _provider.CreateHttpClient())
            {
                SetAcceptHeader(httpClient);
                var content = new StringContent(body, Encoding.UTF8, bodyContenType);
                var res = await httpClient.PostAsync(_provider.KubernetesServiceEndPoint + apiPath, content, ct);
                res.EnsureSuccessStatusCode();
                var responseContent = await res.Content.ReadAsStringAsync();
                return responseContent;
            }
        }

        /// <summary>
        /// Replace resource
        /// </summary>
        /// <param name="apiPath"></param>
        /// <param name="body"></param>
        /// <param name="bodyContenType"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        private async ValueTask<string> PutApiAsync(string apiPath, string body, string bodyContenType = "application/yaml", CancellationToken ct = default)
        {
            using (var httpClient = _provider.CreateHttpClient())
            {
                SetAcceptHeader(httpClient);
                using var content = new StringContent(body, Encoding.UTF8, bodyContenType);
                var res = await httpClient.PutAsync(_provider.KubernetesServiceEndPoint + apiPath, content, ct);
                res.EnsureSuccessStatusCode();
                var responseContent = await res.Content.ReadAsStringAsync();
                return responseContent;
            }
        }

        /// <summary>
        /// <summary>
        /// Delete resource
        /// </summary>
        /// <param name="apiPath"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        private async ValueTask<string> DeleteApiAsync(string apiPath, HttpContent content, CancellationToken ct = default)
        {
            using (var httpClient = _provider.CreateHttpClient())
            {
                SetAcceptHeader(httpClient, "application/json");
                var request = new HttpRequestMessage(HttpMethod.Delete, _provider.KubernetesServiceEndPoint + apiPath)
                {
                    Content = content,
                };
                var res = await httpClient.SendAsync(request, ct);
                res.EnsureSuccessStatusCode();
                var responseContent = await res.Content.ReadAsStringAsync();
                return responseContent;
            }
        }


        /// <summary>
        /// OpenAPI Swagger Definition. https://kubernetes.io/ja/docs/concepts/overview/kubernetes-api/
        /// </summary>
        /// <returns></returns>
        private async ValueTask<string> GetOpenApiSpecAsync()
        {
            using (var httpClient = _provider.CreateHttpClient())
            {
                // must be json. do not set ResponseType
                var apiPath = "/openapi/v2";
                var res = await httpClient.GetStringAsync(_provider.KubernetesServiceEndPoint + apiPath);
                return res;
            }
        }
        #endregion

        private static string Base64ToString(string base64)
        {
            var rentBytes = ArrayPool<byte>.Shared.Rent(Base64.GetMaxDecodedFromUtf8Length(base64.Length));
            try
            {
                Span<byte> base64Bytes = UTF8Encoding.UTF8.GetBytes(base64);
                Span<byte> bytes = rentBytes.AsSpan();
                Base64.DecodeFromUtf8(base64Bytes, bytes, out var bytesComsumed, out var bytesWritten);
                bytes = bytes.Slice(0, bytesWritten);
                return UTF8Encoding.UTF8.GetString(bytes);
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(rentBytes);
            }
        }

        private static IKubernetesClient GetDefaultProvider()
        {
            return Environment.OSVersion.Platform == PlatformID.Unix
                ? (IKubernetesClient)new UnixKubernetesClient()
                : (IKubernetesClient)new WindowsKubernetesClient();
        }
        private void SetProviderConfig()
        {
            _provider.SkipCertificationValidation = _config.SkipCertificateValidation;
        }

        private void SetAcceptHeader(HttpClient httpClient, string acceptHeader)
        {
            if (string.IsNullOrEmpty(acceptHeader))
            {
                SetAcceptHeader(httpClient);
            }
            else
            {
                httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue(acceptHeader));
            }
        }
        private void SetAcceptHeader(HttpClient httpClient)
        {
            switch (_config.ResponseHeaderType)
            {
                case HeaderContentType.Json:
                    httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                    break;
                case HeaderContentType.Yaml:
                    httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/yaml"));
                    break;
                case HeaderContentType.Protobuf:
                    httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.kubernetes.protobuf"));
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(HeaderContentType));
            }
        }
    }
}
