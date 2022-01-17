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
        /// Generate Kubernetes Deployment manifest
        /// </summary>
        /// <remarks>
        /// ---
        /// apiVersion: apps/v1
        /// kind: Deployment
        /// metadata:
        ///   name: {name}
        ///   labels:
        ///     app: {name}
        /// spec:
        ///   replicas: {replicas}
        ///   selector:
        ///     matchLabels:
        ///       app: {name}
        ///   template:
        ///     metadata:
        ///       labels:
        ///         app: {name}
        ///     spec:
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
        ///       nodeSelector:
        ///         eks.amazonaws.com/capacityType: SPOT
        /// </remarks>
        /// <param name="name"></param>
        /// <param name="image"></param>
        /// <param name="imageTag"></param>
        /// <param name="host"></param>
        /// <param name="port"></param>
        /// <param name="imagePullPolicy"></param>
        /// <param name="imagePullSecret"></param>
        /// <param name="replicas"></param>
        /// <param name="nodeSelector"></param>
        /// <returns></returns>
        public V1Deployment CreateDeploymentDefinition(
            string name, 
            string image, 
            string imageTag, 
            string host, 
            int port, 
            string imagePullPolicy = "IfNotPresent", 
            string imagePullSecret = "", 
            int replicas = 1, 
            IDictionary<string, string> nodeSelector = null)
        {
            var labels = new Dictionary<string, string>
            {
                { "app", name },
            };
            var definition = new V1Deployment
            {
                ApiVersion = "apps/v1",
                Kind = "Deployment",
                Metadata = new V1ObjectMeta
                {
                    Name = name,
                    Labels = labels
                },
                Spec = new V1DeploymentSpec
                {
                    Replicas = replicas,
                    Selector = new V1LabelSelector
                    {
                        MatchLabels = labels
                    },
                    Template = new V1PodTemplateSpec
                    {
                        Metadata = new V1ObjectMeta
                        {
                            Labels = labels,
                        },
                        Spec = new V1PodSpec
                        {
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

        public async ValueTask<V1Deployment> CreateDeploymentAsync(string ns, V1Deployment deployment, CancellationToken ct = default)
        {
            using var res = await CreateDeploymentHttpAsync(ns, JsonConvert.Serialize(deployment), ct).ConfigureAwait(false);
            return res.Body;
        }
        public async ValueTask<V1DeploymentList> GetDeploymentsAsync(string ns, string labelSelectorParameter = null, int? timeoutSecondsParameter = null)
        {
            using var res = await GetDeploymentsHttpAsync(ns, false, labelSelectorParameter, timeoutSecondsParameter).ConfigureAwait(false);
            return res.Body;
        }
        public async ValueTask<V1Deployment> GetDeploymentAsync(string ns, string name, string labelSelectorParameter = null, int? timeoutSecondsParameter = null)
        {
            using var res = await GetDeploymentHttpAsync(ns, name, labelSelectorParameter, timeoutSecondsParameter).ConfigureAwait(false);
            return res.Body;
        }
        public async ValueTask<V1Status> DeleteDeploymentAsync(string ns, string name, long? gracePeriodSeconds, string labelSelectorParameter = null, int? timeoutSecondsParameter = null, CancellationToken ct = default)
        {
            // Deployment's REST default deletion propagationPolicy is Orphan.
            // let's use Foreground to avoid pod remains after Deployment deletion.
            var options = new V1DeleteOptions
            {
                GracePeriodSeconds = gracePeriodSeconds,
            };
            using var res = await DeleteDeploymentHttpAsync(ns, name, options, ct, labelSelectorParameter, timeoutSecondsParameter).ConfigureAwait(false);
            return res.Body;
        }
        public async ValueTask<bool> ExistsDeploymentAsync(string ns, string name, string labelSelectorParameter = null, int? timeoutSecondsParameter = null)
        {
            using var exists = await ExistsDeploymentHttpAsync(ns, name, labelSelectorParameter, timeoutSecondsParameter).ConfigureAwait(false);
            return exists.Body;
        }

        #region http
        public async ValueTask<HttpResponse<V1Deployment>> CreateDeploymentHttpAsync(string ns, string manifest, CancellationToken ct = default)
        {
            var res = await PostApiAsync($"/apis/apps/v1/namespaces/{ns}/deployments", null, manifest, ct: ct).ConfigureAwait(false);
            var deploy = JsonConvert.Deserialize<V1Deployment>(res.Content);
            return new HttpResponse<V1Deployment>(deploy)
            {
                Response = res.HttpResponseMessage,
            };
        }
        public async ValueTask<HttpResponse<V1DeploymentList>> GetDeploymentsHttpAsync(string ns, bool watch = false, string labelSelectorParameter = null, int? timeoutSecondsParameter = null, CancellationToken ct = default)
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

            var res = await GetApiAsync($"/apis/apps/v1/namespaces/{ns}/deployments", query, ct: ct).ConfigureAwait(false);
            var deployments = JsonConvert.Deserialize<V1DeploymentList>(res.Content);
            return new HttpResponse<V1DeploymentList>(deployments)
            {
                Response = res.HttpResponseMessage,
            };
        }
        public async ValueTask<HttpResponse<V1Deployment>> GetDeploymentHttpAsync(string ns, string name, string labelSelectorParameter = null, int? timeoutSecondsParameter = null, CancellationToken ct = default)
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

            var res = await GetApiAsync($"/apis/apps/v1/namespaces/{ns}/deployments/{name}", query, ct: ct).ConfigureAwait(false);
            var deployment = JsonConvert.Deserialize<V1Deployment>(res.Content);
            return new HttpResponse<V1Deployment>(deployment)
            {
                Response = res.HttpResponseMessage,
            };
        }
        public async ValueTask<HttpResponse<V1Status>> DeleteDeploymentHttpAsync(string ns, string name, V1DeleteOptions options = null, CancellationToken ct = default, string labelSelectorParameter = null, int? timeoutSecondsParameter = null)
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

            var res = await DeleteApiAsync($"/apis/apps/v1/namespaces/{ns}/deployments/{name}", query, options, ct).ConfigureAwait(false);
            var status = JsonConvert.Deserialize<V1Status>(res.Content);
            return new HttpResponse<V1Status>(status)
            {
                Response = res.HttpResponseMessage,
            };
        }
        public async ValueTask<HttpResponse<bool>> ExistsDeploymentHttpAsync(string ns, string name, string labelSelectorParameter = null, int? timeoutSecondsParameter = null, CancellationToken ct = default)
        {
            try
            {
                var deployments = await GetDeploymentsHttpAsync(ns, false, labelSelectorParameter, timeoutSecondsParameter, ct: ct).ConfigureAwait(false);
                if (deployments == null || !deployments.Body.Items.Any())
                {
                    return new HttpResponse<bool>(false)
                    {
                        Response = deployments.Response,
                    };
                }
                else
                {
                    var exists = deployments.Body.Items.Select(x => x.Metadata.Name == name).Any();
                    return new HttpResponse<bool>(exists)
                    {
                        Response = deployments.Response,
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
