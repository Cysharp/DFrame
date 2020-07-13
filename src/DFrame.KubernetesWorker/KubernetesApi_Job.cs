using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace DFrame.KubernetesWorker
{
    public partial class KubernetesApi
    {
        #region job
        public async ValueTask<string> CreateJobAsync(string @namespace, string manifest, CancellationToken ct = default)
        {
            var res = await PostApiAsync($"/apis/batch/v1/namespaces/{@namespace}/jobs", manifest, "application/yaml", ct);
            return res;
        }
        public async ValueTask<string> GetJobsAsync(string @namespace)
        {
            var res = await GetApiAsync($"/apis/batch/v1/namespaces/{@namespace}/jobs", "application/yaml");
            return res;
        }
        public async ValueTask<string> GetJobAsync(string @namespace, string name)
        {
            var res = await GetApiAsync($"/apis/batch/v1/namespaces/{@namespace}/jobs/{name}", "application/yaml");
            return res;
        }
        public async ValueTask<bool> ExistsJobAsync(string @namespace, string name)
        {
            try
            {
                // 雑 of 雑
                var res = await GetApiAsync($"/apis/batch/v1/namespaces/{@namespace}/jobs/{name}", "application/yaml");
                return true;
            }
            catch (System.Exception)
            {
                return false;
            }
        }
        public async ValueTask<string> DeleteJobAsync(string @namespace, string name, int graceperiodSecond = 0, CancellationToken ct = default)
        {
            // job's REST default deletion propagationPolicy is Orphan.
            // let's use Foreground to avoid pod remains after job deletion.
            var body = new KubernetesJobDeleteBody
            {
                propagationPolicy = "Foreground",
                gracePeriodSeconds = graceperiodSecond,
            };
            var json = JsonSerializer.Serialize(body);
            Console.WriteLine(json);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var res = await DeleteApiAsync($"/apis/batch/v1/namespaces/{@namespace}/jobs/{name}", content, ct);
            return res;
        }
        #endregion
    }

    public class KubernetesJobDeleteBody
    {
        public string apiVersion { get; set; } = "batch/v1";
        public int gracePeriodSeconds { get; set; }
        public string kind { get; set; } = "DeleteOptions";
        public string propagationPolicy { get; set; } = "Foreground"; // Orphan, Background, Foreground
    }
}
