using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using DFrame.Kubernetes.Models;

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
        /// </remarks>
        /// <param name="name"></param>
        /// <param name="image"></param>
        /// <param name="imageTag"></param>
        /// <param name="host"></param>
        /// <param name="port"></param>
        /// <param name="imagePullPolicy"></param>
        /// <param name="imagePullSecret"></param>
        /// <param name="replicas"></param>
        /// <returns></returns>
        public V1Deployment CreateDeploymentDefinition(string name, string image, string imageTag, string host, int port, string imagePullPolicy = "IfNotPresent", string imagePullSecret = "", int replicas = 1)
        {
            var labels = new Dictionary<string, string>
            {
                { "app", name },
            };
            var definition = new V1Deployment
            {
                apiVersion = "apps/v1",
                kind = "Deployment",
                metadata = new V1ObjectMeta
                {
                    name = name,
                    labels = labels
                },
                spec = new V1DeploymentSpec
                {
                    replicas = replicas,
                    selector = new V1LabelSelector
                    {
                        matchLabels = labels
                    },
                    template = new V1PodTemplateSpec
                    {
                        metadata = new V1ObjectMeta
                        {
                            labels = labels,
                        },
                        spec = new V1PodSpec
                        {
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

        public async ValueTask<V1Deployment> CreateDeploymentAsync(string @namespace, V1Deployment deployment, CancellationToken ct = default)
        {
            var res = await CreateDeploymentHttpAsync(@namespace, JsonSerializer.Serialize(deployment), ct);
            var created = JsonSerializer.Deserialize<V1Deployment>(res);
            return created;
        }
        public async ValueTask<V1DeploymentList> GetDeploymentsAsync(string @namespace)
        {
            var res = await GetDeploymentsHttpAsync(@namespace);
            var list = JsonSerializer.Deserialize<V1DeploymentList>(res);
            return list;
        }
        public async ValueTask<V1Deployment> GetDeploymentAsync(string @namespace, string name)
        {
            var res = await GetDeploymentHttpAsync(@namespace, name);
            var get = JsonSerializer.Deserialize<V1Deployment>(res);
            return get;
        }
        public async ValueTask<string> DeleteDeploymentAsync(string @namespace, string name, long? gracePeriodSeconds, CancellationToken ct = default)
        {
            // Deployment's REST default deletion propagationPolicy is Orphan.
            // let's use Foreground to avoid pod remains after Deployment deletion.
            var options = new V1DeleteOptions
            {
                gracePeriodSeconds = gracePeriodSeconds,
            };
            var res = await DeleteDeploymentHttpAsync(@namespace, name, options, ct);
            return res;
        }
        public async ValueTask<bool> ExistsDeploymentAsync(string @namespace, string name)
        {
            var deployments = await GetDeploymentsAsync(@namespace);
            if (deployments == null || !deployments.items.Any()) return false;
            return deployments.items.Select(x => x.metadata.name == name).Any();
        }

        #region api
        public async ValueTask<string> CreateDeploymentHttpAsync(string @namespace, string manifest, CancellationToken ct = default)
        {
            var res = await PostApiAsync($"/apis/apps/v1/namespaces/{@namespace}/deployments", manifest,ct: ct);
            return res;
        }
        public async ValueTask<string> GetDeploymentsHttpAsync(string @namespace)
        {
            var res = await GetApiAsync($"/apis/apps/v1/namespaces/{@namespace}/deployments");
            return res;
        }
        public async ValueTask<string> GetDeploymentHttpAsync(string @namespace, string name)
        {
            var res = await GetApiAsync($"/apis/apps/v1/namespaces/{@namespace}/deployments/{name}");
            return res;
        }
        public async ValueTask<bool> ExistsDeploymentHttpAsync(string @namespace, string name)
        {
            var deployments = await GetDeploymentsAsync(@namespace);
            if (deployments == null || !deployments.items.Any()) return false;
            return deployments.items.Select(x => x.metadata.name == name).Any();
        }
        public async ValueTask<string> DeleteDeploymentHttpAsync(string @namespace, string name, V1DeleteOptions options, CancellationToken ct = default)
        {
            var json = JsonSerializer.Serialize(options);
            using var content = new StringContent(json, Encoding.UTF8, "application/json");
            var res = await DeleteApiAsync($"/apis/apps/v1/namespaces/{@namespace}/deployments/{name}", content, ct);
            return res;
        }
        #endregion
    }
}
