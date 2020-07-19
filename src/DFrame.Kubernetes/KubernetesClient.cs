using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;

namespace DFrame.KubernetesWorker
{
    public enum HeaderContentType
    {
        Json,
        Yaml,
        Protobuf,
    }

    public interface IKubernetesClient
    {
        string AccessToken { get; }
        string HostName { get; }
        bool IsRunningOnKubernetes { get; }
        string KubernetesServiceEndPoint { get; }
        string Namespace { get; }
        bool SkipCertificationValidation { get; set; }

        HttpClient CreateHttpClient();
    }

    public class WindowsKubernetesClient : KubernetesClientBase
    {
        private bool? _isRunningOnKubernetes;

        public override bool IsRunningOnKubernetes
            => _isRunningOnKubernetes ?? (bool)(_isRunningOnKubernetes = !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("KUBERNETES_SERVICE_HOST")));

        public override string AccessToken
            => throw new NotImplementedException();

        public override string HostName
            => throw new NotImplementedException();

        public override string KubernetesServiceEndPoint
            => throw new NotImplementedException();

        public override string Namespace
            => throw new NotImplementedException();
    }

    public class UnixKubernetesClient : KubernetesClientBase
    {
        private bool? _isRunningOnKubernetes;
        private string _namespace;
        private string _hostName;
        private string _accessToken;
        private string _kubernetesServiceEndPoint;

        public override bool IsRunningOnKubernetes
            => _isRunningOnKubernetes ?? (bool)(_isRunningOnKubernetes = !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("KUBERNETES_SERVICE_HOST")));

        public override string AccessToken
            => _accessToken ?? (_accessToken = File.ReadAllText("/var/run/secrets/kubernetes.io/serviceaccount/token"));

        public override string HostName
            => _hostName ?? (_hostName = Environment.GetEnvironmentVariable("HOSTNAME"));

        public override string KubernetesServiceEndPoint
            => _kubernetesServiceEndPoint ?? (_kubernetesServiceEndPoint = $"https://{Environment.GetEnvironmentVariable("KUBERNETES_SERVICE_HOST")}:{Environment.GetEnvironmentVariable("KUBERNETES_SERVICE_PORT")}");

        public override string Namespace
            => _namespace ?? (_namespace = File.ReadAllText("/var/run/secrets/kubernetes.io/serviceaccount/namespace"));
    }

    public abstract class KubernetesClientBase : IKubernetesClient
    {
        public abstract string AccessToken { get; }
        public abstract string HostName { get; }
        public abstract string Namespace { get; }
        public abstract string KubernetesServiceEndPoint { get; }
        public abstract bool IsRunningOnKubernetes { get; }

        public bool SkipCertificationValidation { get; set; }

        public HttpClient CreateHttpClient()
        {
            var httpClientHandler = new HttpClientHandler();
            var httpClient = new HttpClient(httpClientHandler);

            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", AccessToken);

            if (SkipCertificationValidation)
            {
                httpClientHandler.ServerCertificateCustomValidationCallback = /* HttpClientHandler.DangerousAcceptAnyServerCertificateValidator; */ delegate { return true; };
            }

            return httpClient;
        }
    }
}
