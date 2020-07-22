using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace DFrame.KubernetesWorker
{
    public enum ScalingType
    {
        Deployment,
        Job,
    }

    /// <summary>
    /// Configuable worker environment
    /// </summary>
    public class KubernetesEnvironment
    {
        /// <summary>
        /// Worker scaling type.
        /// </summary>
        public ScalingType ScalingType { get; set; } = ScalingType.Job;
        /// <summary>
        /// Master Host to connect from Worker.
        /// </summary>
        public string ConnectTo { get; set; } = Environment.GetEnvironmentVariable("DFRAME_MASTER_HOST") ?? $"dframe-master.dframe.svc.cluster.local";
        /// <summary>
        /// Worker Kubernetes Resource Name.
        /// </summary>
        public string Name { get; set; } = Environment.GetEnvironmentVariable("DFRAME_WORKER_NAME") ?? "dframe-worker";
        /// <summary>
        /// Image Name for Worker Kubernetes Image.
        /// </summary>
        public string Image { get; set; } = Environment.GetEnvironmentVariable("DFRAME_WORKER_IMAGE_NAME") ?? "";
        /// <summary>
        /// Image Tag for Worker Kubernetes Image.
        /// </summary>
        public string ImageTag { get; set; } = Environment.GetEnvironmentVariable("DFRAME_WORKER_IMAGE_TAG") ?? "";
        /// <summary>
        /// Image PullSecret for Worker Kubernetes Image. default empty.
        /// </summary>
        public string ImagePullSecret { get; set; } = Environment.GetEnvironmentVariable("DFRAME_WORKER_IMAGE_PULL_SECERT") ?? "";
        /// <summary>
        /// Image PullPolicy for Worker Kubernetes Image. default IfNotPresent.
        /// </summary>
        public string ImagePullPolicy { get; set; } = Environment.GetEnvironmentVariable("DFRAME_WORKER_IMAGE_PULL_POLICY") ?? "IfNotPresent";
        /// <summary>
        /// Preserve Worker kubernetes resource after execution. default false.
        /// </summary>
        /// <remarks>
        /// any value => true
        /// null => false
        /// </remarks>
        public bool PreserveWorker { get; set; } = !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("DFRAME_WORKER_PRESERVE"));
    }

    /// <summary>
    /// Kubernetes Scaling Provider offers DFrame Worker runs on Kubernetes Job.
    /// If rbac is enabled on cluster, make sure you have service account / role / rolebindings for dframe-master.
    /// </summary>
    public class KubernetesScalingProvider : IScalingProvider
    {
        private readonly KubernetesApi _kubeapi;
        private readonly KubernetesEnvironment _env;
        private readonly string _ns;

        public KubernetesScalingProvider()
        {
            _kubeapi = new KubernetesApi(new KubernetesApiConfig
            {
                ResponseHeaderType = HeaderContentType.Yaml,
                SkipCertificateValidation = true,
            });
            _env = new KubernetesEnvironment();
            _ns = _kubeapi.Namespace;
        }

        public KubernetesScalingProvider(KubernetesEnvironment kubernetesEnvironment) : base()
        {
            _env = kubernetesEnvironment;
        }

        /// <summary>
        /// Create workers via kubernetes client api.
        /// </summary>
        /// <param name="options"></param>
        /// <param name="nodeCount"></param>
        /// <param name="provider"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task StartWorkerAsync(DFrameOptions options, int processCount, IServiceProvider provider, IFailSignal failSignal, CancellationToken cancellationToken)
        {
            Console.WriteLine($"scale out workers. {_ns}/{_env.Name} {_env.ScalingType}");

            // create worker resource
            switch (_env.ScalingType)
            {
                case ScalingType.Deployment:
                    await CreateDeployment(processCount, cancellationToken);
                    break;
                case ScalingType.Job:
                    await CreateJobAsync(processCount, cancellationToken);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(ScalingType));
            }
        }

        public async ValueTask DisposeAsync()
        {
            Console.WriteLine($"scale in workers. {_ns}/{_env.Name} {_env.ScalingType}");

            // delete worker resource.
            switch (_env.ScalingType)
            {
                case ScalingType.Deployment:
                    if (!_env.PreserveWorker && await _kubeapi.ExistsDeploymentAsync(_ns, _env.Name))
                    {
                        await _kubeapi.DeleteDeploymentAsync(_ns, _env.Name);
                    };
                    break;
                case ScalingType.Job:
                    if (!_env.PreserveWorker && await _kubeapi.ExistsJobAsync(_ns, _env.Name))
                    {
                        await _kubeapi.DeleteJobAsync(_ns, _env.Name, graceperiodSecond:10);
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
            var manifest = KubernetesManifest.GetJob(_env.Name, _env.Image, _env.ImageTag, _env.ConnectTo, _env.ImagePullPolicy, _env.ImagePullSecret, nodeCount);

            try
            {
                // create resource
                _ = await _kubeapi.CreateJobAsync(_ns, manifest, cancellationToken);

                // confirm worker created successfully
                var result = await _kubeapi.GetJobAsync(_ns, _env.Name);
                Console.WriteLine($"Worker successfully created.\n{result}");
            }
            catch (HttpRequestException ex)
            {
                Console.WriteLine($"Failed to create worker on Kubernetes Deployment. {_ns}/{_env.Name}. {ex.ToString()}");
                Console.WriteLine($"Dump requested manifest.\n{manifest}");
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
            var manifest = KubernetesManifest.GetDeployment(_env.Name, _env.Image, _env.ImageTag, _env.ConnectTo, _env.ImagePullPolicy, _env.ImagePullSecret, nodeCount);

            try
            {
                // create resource
                _ = await _kubeapi.CreateDeploymentAsync(_ns, manifest, cancellationToken);

                // confirm worker created successfully
                var result = await _kubeapi.GetDeploymentAsync(_ns, _env.Name);
                Console.WriteLine($"Worker successfully created.\n{result}");
            }
            catch (HttpRequestException ex)
            {
                Console.WriteLine($"Failed to create worker on Kubernetes Deployment. {_ns}/{_env.Name}. {ex.ToString()}");
                Console.WriteLine($"Dump requested manifest.\n{manifest}");
                throw;
            }
        }
    }
}
