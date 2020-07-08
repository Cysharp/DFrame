using System;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using DFrame.Core;

namespace DFrame.KubernetesWorker
{
    public class KubernetesScalingProvider : IScalingProvider
    {
        private readonly KubernetesApi _kubeapi;
        private readonly bool preserveWorker = false; // not recommended

        // master info
        private readonly string masterSvc = "dframe-master";
        private readonly string masterNamespace = "dframe";

        // manifest configuable parameters
        private readonly string _ns = "dframe";
        private readonly string _name = "dframe-worker";
        private readonly string imagePullSecret = "aws-registry";
        private readonly string imagePullPolicy = "Never";

        private string _namespaceManifest;
        private string _deploymentManifest;

        public KubernetesScalingProvider()
        {
            _kubeapi = new KubernetesApi(new KubernetesApiConfig
            {
                ResponseHeaderType = HeaderContentType.Yaml,
                SkipCertificateValidation = true,
            });
        }

        public async Task StartWorkerAsync(DFrameOptions options, int nodeCount, CancellationToken cancellationToken)
        {
            // master が kubernetes で起動している、worker をここで作る。
            // todo: rbac が有効だと service account / role / rolebindings が必要 (role は namespace/deployments/pod の create権限....)
            // nodeCount = replicas

            //// create namespace
            //_namespaceManifest = KubernetesManifest.GetNamespace(_ns);
            //if (!await _kubeapi.ExistsNamespaceAsync(_ns))
            //{
            //    _ = await _kubeapi.CreateNamespaceAsync(_ns, _namespaceManifest, cancellationToken);
            //}

            // create deployment
            _deploymentManifest = KubernetesManifest.GetDeployment(_name, "431046970529.dkr.ecr.ap-northeast-1.amazonaws.com/dframe-k8s", "0.1.0", $"{masterSvc}.{masterNamespace}.svc.cluster.local", imagePullPolicy, imagePullSecret, nodeCount);
            Console.WriteLine(_deploymentManifest);
            _ = await _kubeapi.CreateDeploymentAsync(_ns, _deploymentManifest, cancellationToken);

            // wait kubernetes deployments done.
            var deployresult = await _kubeapi.GetDeploymentAsync(_ns, _name);
            Console.WriteLine(deployresult);
            Console.WriteLine($"deployment create. {_ns}/{_name}");
        }

        public async ValueTask DisposeAsync()
        {
            // delete kubernetes deployments. namespace は master を含むので残す。
            if (!preserveWorker && await _kubeapi.ExistsDeploymentAsync(_ns, _name))
            {
                var delete = await _kubeapi.DeleteDeploymentAsync(_ns, _name);
                Console.WriteLine($"deployment delete. {_ns}/{_name}");
            }
        }
    }
}
