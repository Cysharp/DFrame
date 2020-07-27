using System;
using System.Net.Http;
using System.Text.Json;
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
    /// If rbac is enabled on cluster, you have to prepare service account / role / rolebindings for dframe-master.
    /// </summary>
    public class KubernetesScalingProvider : IScalingProvider
    {
        private readonly Kubernetes _operations;
        private readonly KubernetesEnvironment _env;
        private readonly string _ns;

        public KubernetesScalingProvider()
        {
            _operations = new Kubernetes(new KubernetesApiConfig
            {
                ResponseHeaderType = HeaderContentType.Json,
                SkipCertificateValidation = true,
            });
            _env = new KubernetesEnvironment();
            _ns = _operations.Namespace;
        }

        public KubernetesScalingProvider(KubernetesEnvironment kubernetesEnvironment) : base()
        {
            _env = kubernetesEnvironment;
        }

        public async Task StartWorkerAsync(DFrameOptions options, int processCount, IServiceProvider provider, IFailSignal failSignal, CancellationToken cancellationToken)
        {
            Console.WriteLine($"scale out workers. {_ns}/{_env.Name} {_env.ScalingType}");

            // create worker resource
            switch (_env.ScalingType)
            {
                case ScalingType.Deployment:
                    await CreateDeployment(processCount, options.WorkerConnectToHost, options.WorkerConnectToPort, cancellationToken);
                    break;
                case ScalingType.Job:
                    await CreateJobAsync(processCount, options.WorkerConnectToHost, options.WorkerConnectToPort, cancellationToken);
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
                    if (!_env.PreserveWorker && await _operations.ExistsDeploymentAsync(_ns, _env.Name))
                    {
                        await _operations.DeleteDeploymentAsync(_ns, _env.Name, 10);
                    };
                    break;
                case ScalingType.Job:
                    if (!_env.PreserveWorker && await _operations.ExistsJobAsync(_ns, _env.Name))
                    {
                        await _operations.DeleteJobAsync(_ns, _env.Name, 10);
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
        /// <param name="nodeCount"></param>
        /// <param name="connectToHost"></param>
        /// <param name="connectToPort"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        private async ValueTask CreateJobAsync(int nodeCount, string connectToHost, int connectToPort, CancellationToken cancellationToken)
        {
            var def = _operations.CreateJobDefinition(_env.Name, _env.Image, _env.ImageTag, connectToHost, connectToPort, _env.ImagePullPolicy, _env.ImagePullSecret, nodeCount);
            try
            {
                // create worker
                _ = await _operations.CreateJobAsync(_ns, def, cancellationToken);

                // todo: is 5sec enough?
                // wait begin worker
                await Task.Delay(TimeSpan.FromSeconds(5));

                // confirm worker created successfully
                var result = await _operations.GetJobAsync(_ns, _env.Name);
                if (result.status.failed != null && result.status.failed.Value > 0)
                {
                    throw new InvalidOperationException("DFrame worker got failure launching pod.");
                }
                Console.WriteLine($"Worker {_ns}/{_env.Name} successfully created.");
            }
            catch (HttpRequestException ex)
            {
                Console.WriteLine($"Failed to create worker on Kubernetes Deployment. {_ns}/{_env.Name}. {ex.ToString()}");
                Console.WriteLine($"Dump requested manifest.\n{def}");
                throw;
            }
        }

        /// <summary>
        /// create kubernetes deployment. (not recommended)
        /// retry will happen when worker cause error on scenario. not recommeneded.
        /// </summary>
        /// <param name="nodeCount"></param>
        /// <param name="connectToHost"></param>
        /// <param name="connectToPort"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        private async ValueTask CreateDeployment(int nodeCount, string connectToHost, int connectToPort, CancellationToken cancellationToken)
        {
            var def = _operations.CreateDeploymentDefinition(_env.Name, _env.Image, _env.ImageTag, connectToHost, connectToPort, _env.ImagePullPolicy, _env.ImagePullSecret, nodeCount);
            try
            {
                // create worker
                _ = await _operations.CreateDeploymentAsync(_ns, def, cancellationToken);

                // todo: is 5sec enough?
                // wait begin worker
                await Task.Delay(TimeSpan.FromSeconds(5));

                // confirm worker created successfully
                var result = await _operations.GetDeploymentAsync(_ns, _env.Name);
                if (result.status.unavailableReplicas != null && result.status.unavailableReplicas > 0)
                {
                    throw new InvalidOperationException("DFrame worker got failure launching pod.");
                }
                Console.WriteLine($"Worker {_ns}/{_env.Name} successfully created.");
            }
            catch (HttpRequestException ex)
            {
                Console.WriteLine($"Failed to create worker on Kubernetes Deployment. {_ns}/{_env.Name}. {ex.ToString()}");
                Console.WriteLine($"Dump requested manifest.\n{def}");
                throw;
            }
        }
    }
}
