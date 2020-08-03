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
        public async ValueTask<V1Pod> CreatePodAsync(string ns, V1Pod pod, CancellationToken ct = default)
        {
            using var res = await CreatePodHttpAsync(ns, JsonConvert.Serialize(pod), ct).ConfigureAwait(false);
            return res.Body;
        }
        public async ValueTask<V1PodList> GetPodsAsync(string ns, string labelSelectorParameter = null, int? timeoutSecondsParameter = null)
        {
            using var res = await GetPodsHttpAsync(ns, false, labelSelectorParameter, timeoutSecondsParameter).ConfigureAwait(false);
            return res.Body;
        }
        public async ValueTask<V1Pod> GetPodAsync(string ns, string name, string labelSelectorParameter = null, int? timeoutSecondsParameter = null)
        {
            using var res = await GetPodHttpAsync(ns, name, labelSelectorParameter, timeoutSecondsParameter).ConfigureAwait(false);
            return res.Body;
        }
        public async ValueTask<V1Status> DeletePodAsync(string ns, string name, long? gracePeriodSeconds, string labelSelectorParameter = null, int? timeoutSecondsParameter = null, CancellationToken ct = default)
        {
            // Pod's REST default deletion propagationPolicy is Orphan.
            // let's use Foreground to avoid pod remains after Pod deletion.
            var options = new V1DeleteOptions
            {
                GracePeriodSeconds = gracePeriodSeconds,
            };
            using var res = await DeletePodHttpAsync(ns, name, options, ct, labelSelectorParameter, timeoutSecondsParameter).ConfigureAwait(false);
            return res.Body;
        }
        public async ValueTask<bool> ExistsPodAsync(string ns, string name, string labelSelectorParameter = null, int? timeoutSecondsParameter = null)
        {
            using var exists = await ExistsPodHttpAsync(ns, name, labelSelectorParameter, timeoutSecondsParameter).ConfigureAwait(false);
            return exists.Body;
        }

        #region http
        public async ValueTask<HttpResponse<V1Pod>> CreatePodHttpAsync(string ns, string manifest, CancellationToken ct = default)
        {
            var res = await PostApiAsync($"/apis/apps/v1/namespaces/{ns}/pods", null, manifest,ct: ct).ConfigureAwait(false);
            var deploy = JsonConvert.Deserialize<V1Pod>(res.Content);
            return new HttpResponse<V1Pod>(deploy)
            {
                Response = res.HttpResponseMessage,
            };
        }
        public async ValueTask<HttpResponse<V1PodList>> GetPodsHttpAsync(string ns = "", bool watch = false, string labelSelectorParameter = null, int? timeoutSecondsParameter = null)
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

            var url = string.IsNullOrEmpty(ns)
                ? $"/api/v1/pods"
                : $"/api/v1/namespaces/{ns}/pods";
            var res = await GetApiAsync(url, query).ConfigureAwait(false);
            var pods = JsonConvert.Deserialize<V1PodList>(res.Content);
            return new HttpResponse<V1PodList>(pods)
            {
                Response = res.HttpResponseMessage,
            };
        }
        public async ValueTask<HttpResponse<V1Pod>> GetPodHttpAsync(string ns, string name, string labelSelectorParameter = null, int? timeoutSecondsParameter = null)
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

            var res = await GetApiAsync($"/api/v1/namespaces/{ns}/pods/{name}", query).ConfigureAwait(false);
            var pod = JsonConvert.Deserialize<V1Pod>(res.Content);
            return new HttpResponse<V1Pod>(pod)
            {
                Response = res.HttpResponseMessage,
            };
        }
        public async ValueTask<HttpResponse<V1Status>> DeletePodHttpAsync(string ns, string name, V1DeleteOptions options = null, CancellationToken ct = default, string labelSelectorParameter = null, int? timeoutSecondsParameter = null)
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

            var res = options == null
                ? await DeleteApiAsync($"/api/v1/namespaces/{ns}/pods/{name}", query).ConfigureAwait(false)
                : await DeleteApiAsync($"/api/v1/namespaces/{ns}/pods/{name}", query, options).ConfigureAwait(false);
            var status = JsonConvert.Deserialize<V1Status>(res.Content);
            return new HttpResponse<V1Status>(status)
            {
                Response = res.HttpResponseMessage,
            };
        }
        public async ValueTask<HttpResponse<bool>> ExistsPodHttpAsync(string ns, string name, string labelSelectorParameter = null, int? timeoutSecondsParameter = null)
        {
            var pods = await GetPodsHttpAsync(ns, false, labelSelectorParameter, timeoutSecondsParameter).ConfigureAwait(false);
            if (pods == null || !pods.Body.Items.Any())
            {
                return new HttpResponse<bool>(false)
                {
                    Response = pods.Response,
                };
            }
            else
            {
                var exists = pods.Body.Items.Select(x => x.Metadata.Name == name).Any();
                return new HttpResponse<bool>(exists)
                {
                    Response = pods.Response,
                };
            }
        }
        #endregion
    }
}
