using System;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using DFrame.Kubernetes.Exceptions;
using DFrame.Kubernetes.Internals;
using DFrame.Kubernetes.Models;

namespace DFrame.Kubernetes
{
    public enum ScalingType
    {
        Deployment,
        Job,
    }

    // note: change to annotations is match kubernetes like
    /// <summary>
    /// Configuable worker environment
    /// </summary>
    public class KubernetesEnvironment
    {
        /// <summary>
        /// Worker scaling type.
        /// </summary>
        public ScalingType ScalingType { get; set; } = Enum.Parse<ScalingType>(Environment.GetEnvironmentVariable("DFRAME_WORKER_SCALING_TYPE") ?? "Job");
        /// <summary>
        /// Worker Kubernetes resource name. default dframe-worker
        /// </summary>
        public string Name { get; set; } = Environment.GetEnvironmentVariable("DFRAME_WORKER_NAME") ?? "dframe-worker";
        /// <summary>
        /// Image Tag for Worker Kubernetes pod.
        /// </summary>
        public string Image { get; set; } = Environment.GetEnvironmentVariable("DFRAME_WORKER_IMAGE_NAME") ?? "";
        /// <summary>
        /// Image Tag for Worker Kubernetes pod.
        /// </summary>
        public string ImageTag { get; set; } = Environment.GetEnvironmentVariable("DFRAME_WORKER_IMAGE_TAG") ?? "";
        /// <summary>
        /// Image PullSecret for Worker Kubernetes pod. default empty.
        /// </summary>
        public string ImagePullSecret { get; set; } = Environment.GetEnvironmentVariable("DFRAME_WORKER_IMAGE_PULL_SECERT") ?? "";
        /// <summary>
        /// Image PullPolicy for Worker Kubernetes pod. default IfNotPresent.
        /// </summary>
        public string ImagePullPolicy { get; set; } = Environment.GetEnvironmentVariable("DFRAME_WORKER_IMAGE_PULL_POLICY") ?? "IfNotPresent";
        /// <summary>
        /// ServiceAccount for Worker Kubernetes Pod.
        /// Environment Variables sample: DFRAME_WORKER_SERVICEACCOUNT='foo-serviceaccount'
        /// </summary>
        public string ServiceAccount { get; set; } = Environment.GetEnvironmentVariable("DFRAME_WORKER_SERVICEACCOUNT") ?? "";
        /// <summary>
        /// NodeSelector for Worker Kubernetes Pod.
        /// Environment Variables sample: DFRAME_WORKER_NODESELECTOR='KEY1=FOO;KEY2=BAR'
        /// </summary>
        public IDictionary<string, string> NodeSelector { get; set; } = new EnvironmentVariablesSource(string.Empty).GetNodeSelectors("DFRAME_WORKER_NODESELECTOR");
        /// <summary>
        /// Resources.Limits for Worker Kubernetes Pod.
        /// Environment Variables sample: DFRAME_WORKER_RESOURCES_LIMITS='cpu=2000m;memory=1000Mi'
        /// </summary>
        public IDictionary<string, string> ResourcesLimits { get; set; } = new EnvironmentVariablesSource(string.Empty).GetResources("DFRAME_WORKER_RESOURCES_LIMITS");
        /// <summary>
        /// Resources.Requests for Worker Kubernetes Pod.
        /// Environment Variables sample: DFRAME_WORKER_RESOURCES_REQUESTS='cpu=2000m;memory=1000Mi'
        /// </summary>
        public IDictionary<string, string> ResourcesRequests { get; set; } = new EnvironmentVariablesSource(string.Empty).GetResources("DFRAME_WORKER_RESOURCES_REQUESTS");
        /// <summary>
        /// Wait worker pod creationg timeout seconds. default 120 sec.
        /// </summary>
        public int WorkerPodCreationTimeout { get; set; } = int.Parse(Environment.GetEnvironmentVariable("DFRAME_WORKER_POD_CREATE_TIMEOUT") ?? "120");
        /// <summary>
        /// Cluster endpoint health check retry count.
        /// </summary>
        public int ClusterEndpointHealthRetry { get; set; } = int.Parse(Environment.GetEnvironmentVariable("DFRAME_CLUSTER_ENDPOINT_HEALTH_RETRY") ?? "60");
        /// <summary>
        /// Cluster endpoint health check interval second.
        /// </summary>
        public int ClusterEndpointHealthInterval { get; set; } = int.Parse(Environment.GetEnvironmentVariable("DFRAME_CLUSTER_ENDPOINT_HEALTH_INTERVAL_SEC") ?? "10");
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
        IFailSignal _failSignal = default!;

        public KubernetesScalingProvider()
        {
            _env = new KubernetesEnvironment();
            _operations = new Kubernetes(new KubernetesApiConfig
            {
                ResponseHeaderType = HeaderContentType.Json,
                SkipCertificateValidation = true,
            });
            _ns = _operations.Namespace;
        }

        public KubernetesScalingProvider(KubernetesEnvironment kubernetesEnvironment)
        {
            _env = kubernetesEnvironment;
            _operations = new Kubernetes(new KubernetesApiConfig
            {
                ResponseHeaderType = HeaderContentType.Json,
                SkipCertificateValidation = true,
            });
            _ns = _operations.Namespace;
        }

        public async Task StartWorkerAsync(DFrameOptions options, int workerCount, IServiceProvider provider, IFailSignal failSignal, CancellationToken cancellationToken)
        {
            _failSignal = failSignal;

            Console.WriteLine($"Scale out workers {_env.ScalingType}. {_ns}/{_env.Name} ({workerCount} pods)");

            // confirm kubernetes master can connect with cluster api.
            Console.WriteLine($"Checking cluster Endpoint health.");
            var healthy = await _operations.TryConnectClusterEndpointAsync(_env.ClusterEndpointHealthRetry, TimeSpan.FromSeconds(_env.ClusterEndpointHealthInterval), cancellationToken);

            if (!healthy)
            {
                Console.WriteLine($"Cluster endpoint is unhealthy, quiting scale out.");
                _failSignal.TrySetException(new KubernetesException("Could not connect to Kubernetes Cluster Endpoint. Make sure pod can communicate with cluster api."));
            }
            else
            {
                // create worker resource
                switch (_env.ScalingType)
                {
                    case ScalingType.Deployment:
                        await ScaleoutDeploymentAsync(workerCount, options.WorkerConnectToHost, options.WorkerConnectToPort, cancellationToken);
                        break;
                    case ScalingType.Job:
                        await ScaleoutJobAsync(workerCount, options.WorkerConnectToHost, options.WorkerConnectToPort, cancellationToken);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(ScalingType));
                }
            }
        }

        public async ValueTask DisposeAsync()
        {
            Console.WriteLine($"Scale in workers {_env.ScalingType}. {_ns}/{_env.Name}");
            if (!_env.PreserveWorker)
            {
                // delete worker resource.
                switch (_env.ScalingType)
                {
                    case ScalingType.Deployment:
                        var deployExists = await _operations.ExistsDeploymentAsync(_ns, _env.Name);
                        if (deployExists)
                        {
                            await _operations.DeleteDeploymentAsync(_ns, _env.Name, 10);
                        }
                        break;
                    case ScalingType.Job:
                        var jobExists = await _operations.ExistsJobAsync(_ns, _env.Name);
                        if (jobExists)
                        {
                            await _operations.DeleteJobAsync(_ns, _env.Name, 10);
                        }
                        break;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(ScalingType));
                }
            }
            else
            {
                Console.WriteLine($"Detected preserve worker, scale in action skipped.");
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
            var def = _operations.CreateJobDefinition(
                _env.Name,
                _env.Image,
                _env.ImageTag,
                connectToHost,
                connectToPort,
                _env.ImagePullPolicy,
                _env.ImagePullSecret,
                nodeCount,
                _env.ServiceAccount,
                _env.NodeSelector,
                _env.ResourcesLimits,
                _env.ResourcesRequests
            );

            try
            {
                // watch worker pod creation.
                var added = 0;
                var pods = _operations.GetPodsHttpAsync(_ns, true, "app=dframe-worker");
                using (var cts = new CancellationTokenSource(TimeSpan.FromSeconds(_env.WorkerPodCreationTimeout)))
                using (var watch = pods.Watch<V1Pod, V1PodList>((type, item, cts) =>
                {
                    //Console.WriteLine($"pod event caught. {type} {item?.Metadata?.Name}");
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
                ex => throw new KubernetesException($"Kubernetes could not confirm launching desired count of worker pods.", ex),
                () => Console.WriteLine($"Kubernetes pod watch completed. expected {nodeCount}, result {added}"),
                cts))
                {
                    // begin watch
                    var watchTask = watch.Execute();

                    // create worker
                    Console.WriteLine($"Worker creation begin.");
                    var createWorkerTask = await _operations.CreateJobAsync(_ns, def, cancellationToken);

                    // wait watch complete
                    Console.WriteLine($"Worker created, waiting worker scale out for {_env.WorkerPodCreationTimeout}sec");
                    await watchTask;
                }

                // confirm result
                var workerJob = await _operations.GetJobAsync(_ns, _env.Name);
                if (workerJob.Status.Failed != null && workerJob.Status.Failed.Value > 0)
                    throw new KubernetesException($"Failed to scale out worker on kubernetes, job status was failed.");
                var workerPods = await _operations.GetPodsAsync(_ns, "app=dframe-worker");
                var terminatedWorkers = workerPods.Items.Where(x => x.Status?.ContainerStatuses?.FirstOrDefault()?.LastState?.Terminated != null).ToArray();
                if (terminatedWorkers.Any())
                    throw new KubernetesException($"Failed to scale out worker on kubernetes, {terminatedWorkers.Length} pods status detected terminated.");

                Console.WriteLine($"Successfully scale out worker job {_ns}/{_env.Name}.");
            }
            catch (HttpRequestException ex)
            {
                Console.WriteLine($"Failed to create worker on kubernetes. {_ns}/{_env.Name}. {ex}");
                Console.WriteLine($"Dump requested manifest.\n{def}");
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
            var def = _operations.CreateDeploymentDefinition(
                _env.Name,
                _env.Image,
                _env.ImageTag,
                connectToHost,
                connectToPort,
                _env.ImagePullPolicy,
                _env.ImagePullSecret,
                nodeCount,
                _env.ServiceAccount,
                _env.NodeSelector,
                _env.ResourcesLimits,
                _env.ResourcesRequests);
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
                        Console.WriteLine($"{type} {item?.Metadata?.Name} ({added}/{nodeCount})");
                        added++;
                        if (added >= nodeCount)
                        {
                            // complete
                            cts.Cancel();
                        }
                    }
                },
                ex => throw new KubernetesException($"Kubernetes could not confirm launching desired count of worker pods.", ex),
                () => Console.WriteLine($"Kubernetes pod watch completed. expected {nodeCount}, result {added}"),
                cts))
                {
                    // begin watch
                    var watchTask = watch.Execute();

                    // create worker
                    Console.WriteLine($"Worker creation begin.");
                    var createWorkerTask = _operations.CreateDeploymentAsync(_ns, def, cancellationToken);

                    // wait watch complete
                    Console.WriteLine($"Worker created, waiting worker scale out for {_env.WorkerPodCreationTimeout}sec");
                    await watchTask;
                }

                // wait 5 sec before getting status.
                await Task.Delay(30 * 1000);

                // confirm result
                var workerDeploy = await _operations.GetDeploymentAsync(_ns, _env.Name);
                if (workerDeploy.Status.UnavailableReplicas != null && workerDeploy.Status.UnavailableReplicas > 0)
                {
                    Console.WriteLine($"Worker replicas status. ReadyReplicas: {workerDeploy.Status.ReadyReplicas}, UnavailableReplicas: {workerDeploy.Status.UnavailableReplicas}");
                    throw new KubernetesException($"Failed to scale out worker on kubernetes, deploy status was failed.");
                }
                var workerPods = await _operations.GetPodsAsync(_ns, "app=dframe-worker");
                var terminatedWorkers = workerPods.Items.Where(x => x.Status?.ContainerStatuses?.FirstOrDefault()?.LastState?.Terminated != null).ToArray();
                if (terminatedWorkers.Any())
                    throw new KubernetesException($"Failed to scale out worker on kubernetes, {terminatedWorkers.Length} pods status detected terminated.");

                Console.WriteLine($"Successfully scale out worker deploy {_ns}/{_env.Name}.");
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
