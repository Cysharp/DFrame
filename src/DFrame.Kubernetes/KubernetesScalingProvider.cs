using System;
using System.Threading;
using System.Threading.Tasks;
using DFrame;

namespace DFrame.KubernetesWorker
{
    public enum ScalingType
    {
        Deployment,
        Job,
    }

    /// <summary>
    /// Configuable worker parameters
    /// </summary>
    internal class WorkerParameters
    {
        private string _connectTo;
        private string _ns;
        private string _name;
        private string _image;
        private string _imageTag;
        private string _imagePullSecret;
        private string _imagePullPolicy;
        private bool? _preserveWorker;

        /// <summary>
        /// Master Host to connect from Worker.
        /// </summary>
        public string ConnectTo => _connectTo ?? (_connectTo = Environment.GetEnvironmentVariable("DFRAME_MASTER_HOST") ?? $"dframe-master.dframe.svc.cluster.local");
        /// <summary>
        /// Worker Kubernetes NameSpace
        /// </summary>
        public string Namespace => _ns ?? (_ns = Environment.GetEnvironmentVariable("DFRAME_WORKER_NAMESPACE") ?? "dframe");
        /// <summary>
        /// Worker Kubernetes Resource Name.
        /// </summary>
        public string Name => _name ?? (_name = Environment.GetEnvironmentVariable("DFRAME_WORKER_NAME") ?? "dframe-worker");
        /// <summary>
        /// Image Name for Worker Kubernetes Image.
        /// </summary>
        public string Image => _image ?? (_image = Environment.GetEnvironmentVariable("DFRAME_WORKER_IMAGE_NAME") ?? "");
        /// <summary>
        /// Image Tag for Worker Kubernetes Image.
        /// </summary>
        public string ImageTag => _imageTag ?? (_imageTag = Environment.GetEnvironmentVariable("DFRAME_WORKER_IMAGE_TAG") ?? "");
        /// <summary>
        /// Image PullSecret for Worker Kubernetes Image. default empty.
        /// </summary>
        public string ImagePullSecret => _imagePullSecret ?? (_imagePullSecret = Environment.GetEnvironmentVariable("DFRAME_WORKER_IMAGE_PULL_SECERT") ?? "");
        /// <summary>
        /// Image PullPolicy for Worker Kubernetes Image. default IfNotPresent.
        /// </summary>
        public string ImagePullPolicy => _imagePullPolicy ?? (_imagePullSecret = Environment.GetEnvironmentVariable("DFRAME_WORKER_IMAGE_PULL_POLICY") ?? "IfNotPresent");
        /// <summary>
        /// Preserve Worker kubernetes resource after execution. default false.
        /// </summary>
        /// <remarks>
        /// any value => true
        /// null => false
        /// </remarks>
        public bool PreserveWorker => _preserveWorker ?? (bool)(_preserveWorker = !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("DFRAME_WORKER_PRESERVE")));
    }

    /// <summary>
    /// Kubernetes Scaling Provider offers DFrame Worker runs on Kubernetes Job.
    /// If rbac is enabled on cluster, make sure you have service account / role / rolebindings for dframe-master.
    /// </summary>
    public class KubernetesScalingProvider : IScalingProvider
    {
        public ScalingType ScalingType { get; } = ScalingType.Job;

        private readonly KubernetesApi _kubeapi;
        private readonly WorkerParameters _parameters;

        public KubernetesScalingProvider()
        {
            _kubeapi = new KubernetesApi(new KubernetesApiConfig
            {
                ResponseHeaderType = HeaderContentType.Yaml,
                SkipCertificateValidation = true,
            });
            _parameters = new WorkerParameters();
        }

        public KubernetesScalingProvider(ScalingType scalingType) : base()
        {
            this.ScalingType = scalingType;
        }

        /// <summary>
        /// Create workers via kubernetes client api.
        /// </summary>
        /// <param name="options"></param>
        /// <param name="nodeCount"></param>
        /// <param name="provider"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task StartWorkerAsync(DFrameOptions options, int nodeCount, IServiceProvider provider, CancellationToken cancellationToken)
        {
            Console.WriteLine($"scale out workers. {_parameters.Namespace}/{_parameters.Name} {ScalingType}");

            // todo: create namespace for the worker. default same namespace.
            //var namespaceManifest = KubernetesManifest.GetNamespace(_ns);
            //if (!await _kubeapi.ExistsNamespaceAsync(_ns))
            //{
            //    _ = await _kubeapi.CreateNamespaceAsync(_ns, namespaceManifest, cancellationToken);
            //}

            // todo: node count を pod とみなしているので、node = vm の修正が必要。
            // その場合、node = k8s node, worker = deploy replicas (job parallism).
            switch (ScalingType)
            {
                case ScalingType.Deployment:
                    await CreateDeployment(nodeCount, cancellationToken);
                    break;
                case ScalingType.Job:
                    await CreateJobAsync(nodeCount, cancellationToken);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(ScalingType));
            }
        }

        public async ValueTask DisposeAsync()
        {
            Console.WriteLine($"scale in workers. {_parameters.Namespace}/{_parameters.Name} {ScalingType}");

            // delete worker resource. namespace は master を含むので残す。
            switch (ScalingType)
            {
                case ScalingType.Deployment:
                    if (!_parameters.PreserveWorker && await _kubeapi.ExistsDeploymentAsync(_parameters.Namespace, _parameters.Name))
                    {
                        await _kubeapi.DeleteDeploymentAsync(_parameters.Namespace, _parameters.Name);
                    };
                    break;
                case ScalingType.Job:
                    if (!_parameters.PreserveWorker && await _kubeapi.ExistsJobAsync(_parameters.Namespace, _parameters.Name))
                    {
                        await _kubeapi.DeleteJobAsync(_parameters.Namespace, _parameters.Name, graceperiodSecond:10);
                    };
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(ScalingType));
            }
        }

        /// <summary>
        /// create kubernetes job. (recommended)
        /// retry will not happen when worker cause error on scenario.
        /// </summary>
        /// <param name="image"></param>
        /// <param name="imageTag"></param>
        /// <param name="connectTo"></param>
        /// <param name="nodeCount"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        private async ValueTask CreateJobAsync(int nodeCount, CancellationToken cancellationToken)
        {
            Console.WriteLine($"use job. {_parameters.Namespace}/{_parameters.Name}");
            var jobManifest = KubernetesManifest.GetJob(_parameters.Name, _parameters.Image, _parameters.ImageTag, _parameters.ConnectTo, _parameters.ImagePullPolicy, _parameters.ImagePullSecret, nodeCount);
            // Debug Log
            // Console.WriteLine(jobManifest);

            _ = await _kubeapi.CreateJobAsync(_parameters.Namespace, jobManifest, cancellationToken);

            try
            {
                // confirm worker created successfully
                var result = await _kubeapi.GetJobAsync(_parameters.Namespace, _parameters.Name);
                Console.WriteLine($"Worker successfully created.\n{result}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to create worker on Kubernetes Deployment. {_parameters.Namespace}/{_parameters.Name}. {ex.ToString()}");
                throw;
            }
        }

        /// <summary>
        /// create kubernetes deployment. (not recommended)
        /// retry will happen when worker cause error on scenario. not recommeneded.
        /// </summary>
        /// <param name="image"></param>
        /// <param name="imageTag"></param>
        /// <param name="connectTo"></param>
        /// <param name="nodeCount"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        private async ValueTask CreateDeployment(int nodeCount, CancellationToken cancellationToken)
        {
            Console.WriteLine($"use deployment. {_parameters.Namespace}/{_parameters.Name}");
            var deploymentManifest = KubernetesManifest.GetDeployment(_parameters.Name, _parameters.Image, _parameters.ImageTag, _parameters.ConnectTo, _parameters.ImagePullPolicy, _parameters.ImagePullSecret, nodeCount);
            // Debug Log
            // Console.WriteLine(deploymentManifest);

            _ = await _kubeapi.CreateDeploymentAsync(_parameters.Namespace, deploymentManifest, cancellationToken);

            try
            {
                // confirm worker created successfully
                var result = await _kubeapi.GetDeploymentAsync(_parameters.Namespace, _parameters.Name);
                Console.WriteLine($"Worker successfully created.\n{result}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to create worker on Kubernetes Deployment. {_parameters.Namespace}/{_parameters.Name}. {ex.ToString()}");
                throw;
            }
        }
    }
}
