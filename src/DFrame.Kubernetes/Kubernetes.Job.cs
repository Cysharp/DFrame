using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using DFrame.Kubernetes.Models;
using DFrame.Kubernetes.Responses;
using DFrame.Kubernetes.Serializers;

namespace DFrame.Kubernetes
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

        public async ValueTask<V1Job> CreateJobAsync(string ns, V1Job job, CancellationToken ct = default)
        {
            using var res = await CreateJobHttpAsync(ns, JsonConvert.Serialize(job), ct).ConfigureAwait(false);
            return res.Body;
        }
        public async ValueTask<V1JobList> GetJobsAsync(string ns)
        {
            using var res = await GetJobsHttpAsync(ns).ConfigureAwait(false);
            return res.Body;
        }
        public async ValueTask<V1Job> GetJobAsync(string ns, string name)
        {
            using var res = await GetJobHttpAsync(ns, name).ConfigureAwait(false);
            return res.Body;
        }
        public async ValueTask<V1Job> DeleteJobAsync(string ns, string name, long? gracePeriodSeconds, CancellationToken ct = default)
        {
            // job's REST default deletion propagationPolicy is Orphan.
            // let's use Foreground to avoid pod remains after job deletion.
            var options = new V1DeleteOptions
            {
                propagationPolicy = "Foreground",
                gracePeriodSeconds = gracePeriodSeconds,
            };
            using var res = await DeleteJobHttpAsync(ns, name, options, ct).ConfigureAwait(false);
            return res.Body;
        }
        public async ValueTask<bool> ExistsJobAsync(string ns, string name)
        {
            using var jobs = await ExistsJobHttpAsync(ns, name).ConfigureAwait(false);
            return jobs.Body;
        }

        #region api
        public async ValueTask<HttpResponse<V1Job>> CreateJobHttpAsync(string ns, string manifest, CancellationToken ct = default)
        {
            var res = await PostApiAsync($"/apis/batch/v1/namespaces/{ns}/jobs", null, manifest, ct: ct).ConfigureAwait(false);
            var job = JsonConvert.Deserialize<V1Job>(res.Content);
            return new HttpResponse<V1Job>(job)
            {
                Response = res.HttpResponseMessage,
            };
        }
        public async ValueTask<HttpResponse<V1JobList>> GetJobsHttpAsync(string ns, bool watch = false, string labelSelectorParameter = null, int? timeoutSecondsParameter = null)
        {
            // build query
            var query = new StringBuilder();
            if (watch)
            {
                AddQueryParameter(query, "watch", "true");
            }
            if (!string.IsNullOrEmpty(labelSelectorParameter))
            {
                AddQueryParameter(query, "labelSelector", labelSelectorParameter);
            }
            if (timeoutSecondsParameter != null)
            {
                AddQueryParameter(query, "timeoutSeconds", timeoutSecondsParameter.Value.ToString());
            }

            var res = await GetApiAsync($"/apis/batch/v1/namespaces/{ns}/jobs", query).ConfigureAwait(false);
            var jobs = JsonConvert.Deserialize<V1JobList>(res.Content);
            return new HttpResponse<V1JobList>(jobs)
            {
                Response = res.HttpResponseMessage,
            };
        }
        public async ValueTask<HttpResponse<V1Job>> GetJobHttpAsync(string ns, string name, string labelSelectorParameter = null, int? timeoutSecondsParameter = null)
        {
            // build query
            var query = new StringBuilder();
            if (!string.IsNullOrEmpty(labelSelectorParameter))
            {
                AddQueryParameter(query, "labelSelector", labelSelectorParameter);
            }
            if (timeoutSecondsParameter != null)
            {
                AddQueryParameter(query, "timeoutSeconds", timeoutSecondsParameter.Value.ToString());
            }

            var res = await GetApiAsync($"/apis/batch/v1/namespaces/{ns}/jobs/{name}", query).ConfigureAwait(false);
            var job = JsonConvert.Deserialize<V1Job>(res.Content);
            return new HttpResponse<V1Job>(job)
            {
                Response = res.HttpResponseMessage,
            };
        }
        public async ValueTask<HttpResponse<V1Job>> DeleteJobHttpAsync(string ns, string name, V1DeleteOptions options, CancellationToken ct = default, string labelSelectorParameter = null, int? timeoutSecondsParameter = null)
        {
            // build query
            var query = new StringBuilder();
            if (!string.IsNullOrEmpty(labelSelectorParameter))
            {
                AddQueryParameter(query, "labelSelector", labelSelectorParameter);
            }
            if (timeoutSecondsParameter != null)
            {
                AddQueryParameter(query, "timeoutSeconds", timeoutSecondsParameter.Value.ToString());
            }

            var res = await DeleteApiAsync($"/apis/batch/v1/namespaces/{ns}/jobs/{name}", query, options, ct).ConfigureAwait(false);
            var job = JsonConvert.Deserialize<V1Job>(res.Content);
            return new HttpResponse<V1Job>(job)
            {
                Response = res.HttpResponseMessage,
            };
        }
        public async ValueTask<HttpResponse<bool>> ExistsJobHttpAsync(string ns, string name, string labelSelectorParameter = null, int? timeoutSecondsParameter = null)
        {
            var jobs = await GetJobsHttpAsync(ns, false, labelSelectorParameter, timeoutSecondsParameter).ConfigureAwait(false);
            if (jobs == null || !jobs.Body.items.Any())
            {
                return new HttpResponse<bool>(false)
                {
                    Response = jobs.Response,
                };
            }
            else
            {
                var exists = jobs.Body.items.Select(x => x.metadata.name == name).Any();
                return new HttpResponse<bool>(exists)
                {
                    Response = jobs.Response,
                };
            }
        }
        #endregion
    }
}
