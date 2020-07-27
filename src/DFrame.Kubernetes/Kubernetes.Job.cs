using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using DFrame.KubernetesWorker.Models;

namespace DFrame.KubernetesWorker
{
    public partial class Kubernetes
    {
        /// <summary>
        /// Generate Kubernetes Job manifest
        /// </summary>
        /// <remarks>
        /// manifest would be equivalant to YAML manifest.
        /// ---
        /// apiVersion: batch/v1
        /// kind: Job
        /// metadata:
        ///   name: {name}
        ///   labels:
        ///     app: {name}
        /// spec:
        ///   parallelism: {parallelism}
        ///   completions: {parallelism}
        ///   backoffLimit: 0
        ///   template:
        ///     metadata:
        ///       labels:
        ///         app: {name}
        ///     spec:
        ///       restartPolicy: Never
        ///       containers:
        ///         - name: {name}
        ///           image: {image}:{imageTag}
        ///           imagePullPolicy: {imagePullPolicy}
        ///           args: [""--worker-flag""]
        ///           env:
        ///             - name: DFRAME_MASTER_CONNECT_TO_HOST
        ///               value: ""{host}""
        ///             - name: DFRAME_MASTER_CONNECT_TO_PORT
        ///               value: ""{port}""
        ///           resources:
        ///             requests:
        ///               cpu: 100m
        ///               memory: 100Mi
        ///             limits:
        ///               cpu: 2000m
        ///               memory: 1000Mi
        ///       imagePullSecrets:
        ///         - name: {imagePullSecret}
        /// ---
        /// </remarks>
        /// <param name="name"></param>
        /// <param name="image"></param>
        /// <param name="imageTag"></param>
        /// <param name="host"></param>
        /// <param name="port"></param>
        /// <param name="imagePullPolicy"></param>
        /// <param name="imagePullSecret"></param>
        /// <param name="parallelism"></param>
        /// <returns></returns>
        public V1Job CreateJobDefinition(string name, string image, string imageTag, string host, int port, string imagePullPolicy = "IfNotPresent", string imagePullSecret = "", int parallelism = 1)
        {
            var labels = new Dictionary<string, string>
            {
                { "app", name },
            };
            var definition = new V1Job
            {
                apiVersion = "batch/v1",
                kind = "Job",
                metadata = new V1ObjectMeta
                {
                    name = name,
                    labels = labels
                },
                spec = new V1JobSpec
                {
                    parallelism = parallelism,
                    completions = parallelism,
                    // note: must be 0 to prevent pod restart during load testing.
                    backoffLimit = 0,
                    template = new V1PodTemplateSpec
                    {
                        metadata = new V1ObjectMeta
                        {
                            labels = labels,
                        },
                        spec = new V1PodSpec
                        {
                            // note: must be Never to prevent pod restart during load testing.
                            restartPolicy = "Never",
                            containers = new[] {
                                new V1Container
                                {
                                    name = name,
                                    image = $"{image}:{imageTag}",
                                    // "IfNotPresent" to reuse existing, "Never" to always use latest image for same tag.
                                    imagePullPolicy = imagePullPolicy,
                                    args = new [] { "--worker-flag" },
                                    env = new []
                                    {
                                        new V1EnvVar
                                        {
                                            name = "DFRAME_MASTER_CONNECT_TO_HOST",
                                            value = host,
                                        },
                                        new V1EnvVar
                                        {
                                            name = "DFRAME_MASTER_CONNECT_TO_PORT",
                                            value = port.ToString(),
                                        }
                                    },
                                    resources = new V1ResourceRequirements
                                    {
                                        // todo: should be configuable
                                        limits = new Dictionary<string, ResourceQuantity>
                                        {
                                            { "cpu", new ResourceQuantity{ value = "2000m" } },
                                            { "memory", new ResourceQuantity{ value = "1000Mi" } },
                                        },
                                        requests = new Dictionary<string, ResourceQuantity>
                                        {
                                            { "cpu", new ResourceQuantity{ value = "100m" } },
                                            { "memory", new ResourceQuantity{ value = "100Mi" } },
                                        }
                                    },
                                },
                            },
                        },
                    }
                }
            };

            if (!string.IsNullOrEmpty(imagePullSecret))
            {
                definition.spec.template.spec.imagePullSecrets = new[]
                {
                    new V1LocalObjectReference
                    {
                        name = imagePullSecret,
                    },
                };
            }
            return definition;
        }

        public async ValueTask<V1Job> CreateJobAsync(string @namespace, V1Job job, CancellationToken ct = default)
        {
            var res = await CreateJobHttpAsync(@namespace, JsonSerializer.Serialize(job), ct);
            var created = JsonSerializer.Deserialize<V1Job>(res);
            return created;
        }
        public async ValueTask<V1JobList> GetJobsAsync(string @namespace)
        {
            var res = await GetJobsHttpAsync(@namespace);
            var list = JsonSerializer.Deserialize<V1JobList>(res);
            return list;
        }
        public async ValueTask<V1Job> GetJobAsync(string @namespace, string name)
        {
            var res = await GetJobHttpAsync(@namespace, name);
            var get = JsonSerializer.Deserialize<V1Job>(res);
            return get;
        }
        public async ValueTask<string> DeleteJobAsync(string @namespace, string name, long? gracePeriodSeconds, CancellationToken ct = default)
        {
            // job's REST default deletion propagationPolicy is Orphan.
            // let's use Foreground to avoid pod remains after job deletion.
            var options = new V1DeleteOptions
            {
                propagationPolicy = "Foreground",
                gracePeriodSeconds = gracePeriodSeconds,
            };
            var res = await DeleteJobHttpAsync(@namespace, name, options, ct);
            return res;
        }
        public async ValueTask<bool> ExistsJobAsync(string @namespace, string name)
        {
            var jobs = await GetJobsAsync(@namespace);
            if (jobs == null || !jobs.items.Any()) return false;
            return jobs.items.Select(x => x.metadata.name == name).Any();
        }

        #region api
        private async ValueTask<string> CreateJobHttpAsync(string @namespace, string manifest, CancellationToken ct = default)
        {
            var res = await PostApiAsync($"/apis/batch/v1/namespaces/{@namespace}/jobs", manifest, ct: ct);
            return res;
        }
        private async ValueTask<string> GetJobsHttpAsync(string @namespace)
        {
            var res = await GetApiAsync($"/apis/batch/v1/namespaces/{@namespace}/jobs");
            return res;
        }
        private async ValueTask<string> GetJobHttpAsync(string @namespace, string name)
        {
            var res = await GetApiAsync($"/apis/batch/v1/namespaces/{@namespace}/jobs/{name}");
            return res;
        }
        private async ValueTask<string> DeleteJobHttpAsync(string @namespace, string name, V1DeleteOptions options, CancellationToken ct = default)
        {
            var json = JsonSerializer.Serialize(options);
            using var content = new StringContent(json, Encoding.UTF8, "application/json");
            var res = await DeleteApiAsync($"/apis/batch/v1/namespaces/{@namespace}/jobs/{name}", content, ct);
            return res;
        }
        #endregion
    }
}
