using System;
using System.Buffers;
using System.Buffers.Text;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using DFrame.Kubernetes.Internals.Responses;
using DFrame.Kubernetes.Models;
using DFrame.Kubernetes.Serializers;

namespace DFrame.Kubernetes
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

        private readonly IKubernetesClient _provider;
        private readonly KubernetesApiConfig _config;

        public Kubernetes()
        {
            _config = new KubernetesApiConfig();
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

        /// <summary>
        /// OpenAPI Swagger Definition. https://kubernetes.io/ja/docs/concepts/overview/kubernetes-api/
        /// </summary>
        /// <returns></returns>
        internal async ValueTask<string> GetOpenApiSpecAsync()
        {
            var apiPath = "/openapi/v2";
            var res = await GetApiAsync(apiPath, null).ConfigureAwait(false);
            return res.Content;
        }

        #region api
        /// <summary>
        /// Get resource
        /// </summary>
        /// <param name="apiPath"></param>
        /// <param name="query"></param>
        /// <param name="acceptHeader"></param>
        /// <returns></returns>
        private async ValueTask<HttpResponseWrapper> GetApiAsync(string apiPath, StringBuilder query, string acceptHeader = default)
        {
            using var httpClient = _provider.CreateHttpClient();
            SetAcceptHeader(httpClient, acceptHeader);
            var url = new UriBuilder(_provider.KubernetesServiceEndPoint + apiPath);
            SetQuery(url, query);
            using var request = new HttpRequestMessage(HttpMethod.Get, url.ToString());
            var res = await httpClient.SendAsync(request).ConfigureAwait(false);
            res.EnsureSuccessStatusCode();

            var responseContent = await res.Content.ReadAsStringAsync();
            return new HttpResponseWrapper(res, responseContent);
        }

        /// <summary>
        /// Create Resource
        /// </summary>
        /// <param name="apiPath"></param>
        /// <param name="query"></param>
        /// <param name="body"></param>
        /// <param name="bodyContenType"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        private async ValueTask<HttpResponseWrapper> PostApiAsync(string apiPath, StringBuilder query, string body, string bodyContenType = "application/json", CancellationToken ct = default)
        {
            using var httpClient = _provider.CreateHttpClient();
            SetAcceptHeader(httpClient);
            var url = new UriBuilder(_provider.KubernetesServiceEndPoint + apiPath);
            SetQuery(url, query);
            using var request = new HttpRequestMessage(HttpMethod.Post, url.ToString())
            {
                Content = new StringContent(body, Encoding.UTF8, bodyContenType),
            };
            var res = await httpClient.SendAsync(request, ct).ConfigureAwait(false);
            res.EnsureSuccessStatusCode();

            var responseContent = await res.Content.ReadAsStringAsync().ConfigureAwait(false);
            return new HttpResponseWrapper(res, responseContent);
        }

        /// <summary>
        /// Replace resource
        /// </summary>
        /// <param name="apiPath"></param>
        /// <param name="query"></param>
        /// <param name="body"></param>
        /// <param name="bodyContenType"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        private async ValueTask<HttpResponseWrapper> PutApiAsync(string apiPath, StringBuilder query, string body, string bodyContenType = "application/json", CancellationToken ct = default)
        {
            using var httpClient = _provider.CreateHttpClient();
            SetAcceptHeader(httpClient);
            var url = new UriBuilder(_provider.KubernetesServiceEndPoint + apiPath);
            SetQuery(url, query);
            using var request = new HttpRequestMessage(HttpMethod.Put, url.ToString())
            {
                Content = new StringContent(body, Encoding.UTF8, bodyContenType),
            };
            var res = await httpClient.SendAsync(request, ct).ConfigureAwait(false);
            res.EnsureSuccessStatusCode();

            var responseContent = await res.Content.ReadAsStringAsync().ConfigureAwait(false);
            return new HttpResponseWrapper(res, responseContent);
        }

        /// <summary>
        /// Delete resource
        /// </summary>
        /// <param name="apiPath"></param>
        /// <param name="query"></param>
        /// <param name="options"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        private async ValueTask<HttpResponseWrapper> DeleteApiAsync(string apiPath, StringBuilder query, V1DeleteOptions options = null, CancellationToken ct = default)
        {
            using var httpClient = _provider.CreateHttpClient();
            SetAcceptHeader(httpClient);
            var url = new UriBuilder(_provider.KubernetesServiceEndPoint + apiPath);
            SetQuery(url, query);
            using var request = new HttpRequestMessage(HttpMethod.Delete, url.ToString());
            if (options != null)
            {
                request.Content = new StringContent(JsonConvert.Serialize(options), Encoding.UTF8, "application/json");
            }
            var res = await httpClient.SendAsync(request, ct).ConfigureAwait(false);
            res.EnsureSuccessStatusCode();

            var responseContent = await res.Content.ReadAsStringAsync().ConfigureAwait(false);
            return new HttpResponseWrapper(res, responseContent);
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

        /// <summary>
        /// build query parameters
        /// </summary>
        /// <param name="sb"></param>
        /// <param name="key"></param>
        /// <param name="value"></param>
        private static void AddQueryParameter(StringBuilder sb, string key, string value)
        {
            if (sb == null)
                throw new ArgumentNullException(nameof(sb));

            if (string.IsNullOrEmpty(key))
                throw new ArgumentNullException(nameof(key));

            sb.Append(sb.Length != 0 ? '&' : '?').Append(Uri.EscapeDataString(key)).Append('=');
            if (!string.IsNullOrEmpty(value))
            {
                sb.Append(Uri.EscapeDataString(value));
            }
        }
        private static void SetQuery(UriBuilder uriBuilder, StringBuilder query)
        {
            if (query != null && query.Length > 0)
            {
                // UriBuilder.Query not accept leading '?', trim it.
                uriBuilder.Query = query.Length == 0
                    ? ""
                    : query.ToString(1, query.Length - 1);
            }
        }
    }
}
