using System;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using DFrame.Kubernetes.Exceptions;
using DFrame.Kubernetes.Models;

namespace DFrame.Kubernetes
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
        /// Worker Kubernetes resource name. default dframe-worker
        /// </summary>
        public string Name { get; set; } = Environment.GetEnvironmentVariable("DFRAME_WORKER_NAME") ?? "dframe-worker";
        /// <summary>
        /// Image Tag for Worker Kubernetes Image.
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
        /// Wait worker pod creationg timeout seconds. default 120 sec.
        /// </summary>
        public int WorkerPodCreationTimeout { get; set; } = int.Parse(Environment.GetEnvironmentVariable("DFRAME_WORKER_POD_CREATE_TIMEOUT") ?? "120");
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
                    await ScaleoutDeploymentAsync(processCount, options.WorkerConnectToHost, options.WorkerConnectToPort, cancellationToken);
                    break;
                case ScalingType.Job:
                    await ScaleoutJobAsync(processCount, options.WorkerConnectToHost, options.WorkerConnectToPort, cancellationToken);
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
        /// scale out with kubernetes job. (recommended)
        /// retry will not happen when worker cause error on worker plan.
        /// </summary>
        /// <param name="nodeCount"></param>
        /// <param name="connectToHost"></param>
        /// <param name="connectToPort"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        private async ValueTask ScaleoutJobAsync(int nodeCount, string connectToHost, int connectToPort, CancellationToken cancellationToken)
        {
            var def = _operations.CreateJobDefinition(_env.Name, _env.Image, _env.ImageTag, connectToHost, connectToPort, _env.ImagePullPolicy, _env.ImagePullSecret, nodeCount);
            try
            {
                // watch worker pod creation.
                var added = 0;
                var pods = _operations.GetPodsHttpAsync(_ns, true, "app=dframe-worker");
                using (var cts = new CancellationTokenSource(TimeSpan.FromSeconds(_env.WorkerPodCreationTimeout)))
                using (var watch = pods.Watch<V1Pod, V1PodList>((type, item, cts) =>
                {
                    if (type == WatchEventType.Added)
                    {
                        added++;
                        if (added >= nodeCount)
                        {
                            // complete
                            cts.Cancel();
                        }
                    }
                },
                ex => throw new KubernetesException($"kubernetes could not confirm launching desired count of worker pods.", ex),
                () => Console.WriteLine($"kubernetes pod watch completed. expected {nodeCount}, result {added}"),
                cts))
                {
                    // begin watch
                    var watchTask = watch.Execute();

                    // create worker
                    var createWorkerTask = await _operations.CreateJobAsync(_ns, def, cancellationToken);

                    // wait watch complete
                    Console.WriteLine($"waiting worker scale out for {_env.WorkerPodCreationTimeout}sec");
                    await watchTask;
                }

                // confirm result
                var workerJob = await _operations.GetJobAsync(_ns, _env.Name);
                if (workerJob.Status.Failed != null && workerJob.Status.Failed.Value > 0)
                    throw new KubernetesException($"failed to scale out worker on kubernetes, job status was failed.");
                var workerPods = await _operations.GetPodsAsync(_ns, "app=dframe-worker");
                var terminatedWorkers = workerPods.Items.Where(x => x.Status?.ContainerStatuses?.FirstOrDefault()?.LastState?.Terminated != null).ToArray();
                if (terminatedWorkers.Any())
                    throw new KubernetesException($"failed to scale out worker on kubernetes, {terminatedWorkers.Length} pods status detected terminated.");

                Console.WriteLine($"successfully scale out worker job {_ns}/{_env.Name}.");
            }
            catch (HttpRequestException ex)
            {
                Console.WriteLine($"failed to create worker on kubernetes. {_ns}/{_env.Name}. {ex}");
                Console.WriteLine($"dump requested manifest.\n{def}");
                throw;
            }
        }

        /// <summary>
        /// scale out with kubernetes deployment. (not recommended)
        /// retry will happen when worker cause error on worker plan.
        /// </summary>
        /// <param name="nodeCount"></param>
        /// <param name="connectToHost"></param>
        /// <param name="connectToPort"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        private async ValueTask ScaleoutDeploymentAsync(int nodeCount, string connectToHost, int connectToPort, CancellationToken cancellationToken)
        {
            var def = _operations.CreateDeploymentDefinition(_env.Name, _env.Image, _env.ImageTag, connectToHost, connectToPort, _env.ImagePullPolicy, _env.ImagePullSecret, nodeCount);
            try
            {
                // watch worker pod creation.
                var added = 0;
                var pods = _operations.GetPodsHttpAsync(_ns, true, "app=dframe-worker");
                using (var cts = new CancellationTokenSource(TimeSpan.FromSeconds(_env.WorkerPodCreationTimeout)))
                using (var watch = pods.Watch<V1Pod, V1PodList>((type, item, cts) =>
                {
                    if (type == WatchEventType.Added)
                    {
                        added++;
                        if (added >= nodeCount)
                        {
                            // complete
                            cts.Cancel();
                        }
                    }
                },
                ex => throw new KubernetesException($"kubernetes could not confirm launching desired count of worker pods.", ex),
                () => Console.WriteLine($"kubernetes pod watch completed. expected {nodeCount}, result {added}"),
                cts))
                {
                    // begin watch
                    var watchTask = watch.Execute();

                    // create worker
                    var createWorkerTask = _operations.CreateDeploymentAsync(_ns, def, cancellationToken);

                    // wait watch complete
                    Console.WriteLine($"waiting worker scale out for {_env.WorkerPodCreationTimeout}sec");
                    await watchTask;
                }

                // confirm result
                var workerDeploy = await _operations.GetDeploymentAsync(_ns, _env.Name);
                if (workerDeploy.Status.UnavailableReplicas != null && workerDeploy.Status.UnavailableReplicas > 0)
                    throw new KubernetesException($"failed to scale out worker on kubernetes, deploy status was failed.");
                var workerPods = await _operations.GetPodsAsync(_ns, "app=dframe-worker");
                var terminatedWorkers = workerPods.Items.Where(x => x.Status?.ContainerStatuses?.FirstOrDefault()?.LastState?.Terminated != null).ToArray();
                if (terminatedWorkers.Any())
                    throw new KubernetesException($"failed to scale out worker on kubernetes, {terminatedWorkers.Length} pods status detected terminated.");

                Console.WriteLine($"successfully scale out worker deploy {_ns}/{_env.Name}.");
            }
            catch (HttpRequestException ex)
            {
                Console.WriteLine($"Failed to create worker on Kubernetes Deployment. {_ns}/{_env.Name}. {ex}");
                Console.WriteLine($"Dump requested manifest.\n{def}");
                throw;
            }
        }
    }
}
