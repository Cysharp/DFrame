using System;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using DFrame.Core;
using YamlDotNet.Serialization;

namespace DFrame.KubernetesWorker
{
    public class KubernetesScalingProvider : IScalingProvider
    {
        private readonly string _ns = "dframe-worker";
        private readonly string _name = "dframe";
        private readonly KubernetesApi _kubeapi;
        private readonly IDeserializer _yamlDeserializer;

        private string _namespaceManifest;
        private string _deploymentManifest;

        public KubernetesScalingProvider()
        {
            _kubeapi = new KubernetesApi(new KubernetesApiConfig
            {
                AccesptHeaderType = HeaderContentType.Yaml,
                SkipCertificateValidation = true,
            });
            _yamlDeserializer = new DeserializerBuilder().Build();
        }

        public async Task StartWorkerAsync(DFrameOptions options, int nodeCount, CancellationToken cancellationToken)
        {
            // master が kubernetes で起動している、worker をここで作る。
            // todo: rbac が有効だと service account / role / rolebindings が必要 (role は namespace/deployments/pod の create権限....)

            // create kuberentes deployments. replicas = nodeCount
            // create namespace
            _namespaceManifest = KubernetesManifest.GetNamespace(_ns);
            _ = await _kubeapi.CreateNamespaceAsync(_ns, _namespaceManifest, cancellationToken);

            // create deployment
            _deploymentManifest = KubernetesManifest.GetDeployment(_name, "cysharp/dframe-worker", "0.1.0", options.Host, nodeCount);
            _ = await _kubeapi.CreateDeploymentAsync(_ns, _deploymentManifest, cancellationToken);

            // wait kubernetes deployments done.
            var deployresult = await _kubeapi.GetDeploymentAsync(_ns, _name);
            var deploy = _yamlDeserializer.Deserialize<KubernetesDeploymentMetadata>(deployresult);
            Console.WriteLine($"deployment create. {deploy.metadata.@namespace}/{deploy.metadata.name}");
        }

        public async ValueTask DisposeAsync()
        {
            // delete kubernetes deployments. namespace は master を含むので残す。
            if (await _kubeapi.ExistsDeploymentAsync(_ns, _name))
            {
                var delete = await _kubeapi.DeleteDeploymentAsync(_ns, _name);
                Console.WriteLine($"deployment delete. {_ns}/{_name}");
            }
        }
    }
}
