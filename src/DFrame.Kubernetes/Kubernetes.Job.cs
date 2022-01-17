using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
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
        /// <param name="nodeSelector"></param>
        /// <returns></returns>
        public V1Job CreateJobDefinition(
            string name, 
            string image, 
            string imageTag, 
            string host, 
            int port, 
            string imagePullPolicy = "IfNotPresent", 
            string imagePullSecret = "", 
            int parallelism = 1,
            IDictionary<string, string> nodeSelector = null)
        {
            var labels = new Dictionary<string, string>
            {
                { "app", name },
            };
            var definition = new V1Job
            {
                ApiVersion = "batch/v1",
                Kind = "Job",
                Metadata = new V1ObjectMeta
                {
                    Name = name,
                    Labels = labels
                },
                Spec = new V1JobSpec
                {
                    Parallelism = parallelism,
                    Completions = parallelism,
                    // note: must be 0 to prevent pod restart during load testing.
                    BackoffLimit = 0,
                    Template = new V1PodTemplateSpec
                    {
                        Metadata = new V1ObjectMeta
                        {
                            Labels = labels,
                        },
                        Spec = new V1PodSpec
                        {
                            // note: must be Never to prevent pod restart during load testing.
                            RestartPolicy = "Never",
                            Containers = new[] {
                                new V1Container
                                {
                                    Name = name,
                                    Image = $"{image}:{imageTag}",
                                    // "IfNotPresent" to reuse existing, "Never" to always use latest image for same tag.
                                    ImagePullPolicy = imagePullPolicy,
                                    Args = new [] { "--worker-flag" },
                                    Env = new []
                                    {
                                        new V1EnvVar
                                        {
                                            Name = "DFRAME_MASTER_CONNECT_TO_HOST",
                                            Value = host,
                                        },
                                        new V1EnvVar
                                        {
                                            Name = "DFRAME_MASTER_CONNECT_TO_PORT",
                                            Value = port.ToString(),
                                        }
                                    },
                                    Resources = new V1ResourceRequirements
                                    {
                                        // todo: should be configuable
                                        Limits = new Dictionary<string, ResourceQuantity>
                                        {
                                            { "cpu", new ResourceQuantity{ Value = "2000m" } },
                                            { "memory", new ResourceQuantity{ Value = "1000Mi" } },
                                        },
                                        Requests = new Dictionary<string, ResourceQuantity>
                                        {
                                            { "cpu", new ResourceQuantity{ Value = "100m" } },
                                            { "memory", new ResourceQuantity{ Value = "100Mi" } },
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
                definition.Spec.Template.Spec.ImagePullSecrets = new[]
                {
                    new V1LocalObjectReference
                    {
                        Name = imagePullSecret,
                    },
                };
            }
            if (nodeSelector != null && nodeSelector.Count != 0)
            {
                definition.Spec.Template.Spec.NodeSelector = nodeSelector;
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
                PropagationPolicy = "Foreground",
                GracePeriodSeconds = gracePeriodSeconds,
            };
            using var res = await DeleteJobHttpAsync(ns, name, options, ct).ConfigureAwait(false);
            return res.Body;
        }
        public async ValueTask<bool> ExistsJobAsync(string ns, string name, CancellationToken ct = default)
        {
            using var jobs = await ExistsJobHttpAsync(ns, name, ct: ct).ConfigureAwait(false);
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
        public async ValueTask<HttpResponse<V1JobList>> GetJobsHttpAsync(string ns, bool watch = false, string labelSelectorParameter = null, int? timeoutSecondsParameter = null, CancellationToken ct = default)
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

            var res = await GetApiAsync($"/apis/batch/v1/namespaces/{ns}/jobs", query, ct: ct).ConfigureAwait(false);
            var jobs = JsonConvert.Deserialize<V1JobList>(res.Content);
            return new HttpResponse<V1JobList>(jobs)
            {
                Response = res.HttpResponseMessage,
            };
        }
        public async ValueTask<HttpResponse<V1Job>> GetJobHttpAsync(string ns, string name, string labelSelectorParameter = null, int? timeoutSecondsParameter = null, CancellationToken ct = default)
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

            var res = await GetApiAsync($"/apis/batch/v1/namespaces/{ns}/jobs/{name}", query, ct: ct).ConfigureAwait(false);
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
        public async ValueTask<HttpResponse<bool>> ExistsJobHttpAsync(string ns, string name, string labelSelectorParameter = null, int? timeoutSecondsParameter = null, CancellationToken ct = default)
        {
            try
            {
                var jobs = await GetJobsHttpAsync(ns, false, labelSelectorParameter, timeoutSecondsParameter, ct: ct).ConfigureAwait(false);
                if (jobs == null || !jobs.Body.Items.Any())
                {
                    return new HttpResponse<bool>(false)
                    {
                        Response = jobs.Response,
                    };
                }
                else
                {
                    var exists = jobs.Body.Items.Select(x => x.Metadata.Name == name).Any();
                    return new HttpResponse<bool>(exists)
                    {
                        Response = jobs.Response,
                    };
                }
            }
            catch (HttpRequestException ex)
            {
                return new HttpResponse<bool>(false)
                {
                    Response = new HttpResponseMessage()
                    {
                        Content = new StringContent($"{ex.GetType().FullName} {ex.Message} {ex.StackTrace}"),
                    },
                };
            }
            catch (TimeoutException tex)
            {
                return new HttpResponse<bool>(false)
                {
                    Response = new HttpResponseMessage()
                    {
                        Content = new StringContent($"{tex.GetType().FullName} {tex.Message} {tex.StackTrace}"),
                    },
                };
            }
            catch (TaskCanceledException taex)
            {
                return new HttpResponse<bool>(false)
                {
                    Response = new HttpResponseMessage()
                    {
                        Content = new StringContent($"{taex.GetType().FullName} {taex.Message} {taex.StackTrace}"),
                    },
                };
            }
        }
        #endregion
    }
}
