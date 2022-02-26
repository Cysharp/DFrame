using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;

namespace DFrame
{
    internal class DefaultWorkloadAttribute : Attribute
    {
    }

    [DefaultWorkload]
    internal abstract class DefaultHttpWorkloadBase : Workload
    {
        protected static readonly HttpClient DefaultHttpClient = new HttpClient(new SocketsHttpHandler
        {
            MaxConnectionsPerServer = int.MaxValue,
            AutomaticDecompression = System.Net.DecompressionMethods.None,
        });

        protected static async Task ReadToEndAsync(Stream source, CancellationToken cancellationToken)
        {
            byte[] buffer = ArrayPool<byte>.Shared.Rent(65536);
            try
            {
                int bytesRead;
                while ((bytesRead = await source.ReadAsync(new Memory<byte>(buffer), cancellationToken).ConfigureAwait(false)) != 0)
                {
                }
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(buffer);
            }
        }

        protected static async Task ReadResponseAsync(HttpResponseMessage response, CancellationToken cancellationToken)
        {
            var stream = await response.EnsureSuccessStatusCode().Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
            await ReadToEndAsync(stream, cancellationToken).ConfigureAwait(false);
        }

        protected static ByteArrayContent CreateJsonContent(string body)
        {
            var content = new ByteArrayContent(Encoding.UTF8.GetBytes(body));
            content.Headers.ContentType = new MediaTypeHeaderValue("application/json") { CharSet = "utf-8" };
            return content;
        }

        protected static FormUrlEncodedContent CreateFormUrlEncodedContent(string body)
        {
            var jsonObject = JsonSerializer.Deserialize<JsonObject>(body);
            var kvps = jsonObject?.Select(x => new KeyValuePair<string, string>(x.Key, x.Value?.GetValue<object>().ToString() ?? "")) ?? Array.Empty<KeyValuePair<string, string>>();
            return new FormUrlEncodedContent(kvps);
        }
    }

    internal class HttpGet : DefaultHttpWorkloadBase
    {
        readonly string url;

        public HttpGet(string url)
        {
            this.url = url;
        }

        public override async Task ExecuteAsync(WorkloadContext context)
        {
            var response = await DefaultHttpClient.GetAsync(url, context.CancellationToken).ConfigureAwait(false);
            await ReadResponseAsync(response, context.CancellationToken).ConfigureAwait(false);
        }
    }

    internal class HttpDelete : DefaultHttpWorkloadBase
    {
        readonly string url;

        public HttpDelete(string url)
        {
            this.url = url;
        }

        public override async Task ExecuteAsync(WorkloadContext context)
        {
            var response = await DefaultHttpClient.DeleteAsync(url, context.CancellationToken).ConfigureAwait(false);
            await ReadResponseAsync(response, context.CancellationToken).ConfigureAwait(false);
        }
    }

    internal class HttpPostJson : DefaultHttpWorkloadBase
    {
        readonly string url;
        readonly ByteArrayContent body;

        public HttpPostJson(string url, string jsonBody)
        {
            this.url = url;
            this.body = CreateJsonContent(jsonBody);
        }

        public override async Task ExecuteAsync(WorkloadContext context)
        {
            var response = await DefaultHttpClient.PostAsync(url, body, context.CancellationToken).ConfigureAwait(false);
            await ReadResponseAsync(response, context.CancellationToken).ConfigureAwait(false);
        }
    }

    internal class HttpPostFormUrlEncoded : DefaultHttpWorkloadBase
    {
        readonly string url;
        readonly FormUrlEncodedContent body;

        public HttpPostFormUrlEncoded(string url, string jsonBody)
        {
            this.url = url;
            this.body = CreateFormUrlEncodedContent(jsonBody);
        }

        public override async Task ExecuteAsync(WorkloadContext context)
        {
            var response = await DefaultHttpClient.PostAsync(url, body, context.CancellationToken).ConfigureAwait(false);
            await ReadResponseAsync(response, context.CancellationToken).ConfigureAwait(false);
        }
    }

    internal class HttpPutJson : DefaultHttpWorkloadBase
    {
        readonly string url;
        readonly ByteArrayContent body;

        public HttpPutJson(string url, string jsonBody)
        {
            this.url = url;
            this.body = CreateJsonContent(jsonBody);
        }

        public override async Task ExecuteAsync(WorkloadContext context)
        {
            var response = await DefaultHttpClient.PutAsync(url, body, context.CancellationToken).ConfigureAwait(false);
            await ReadResponseAsync(response, context.CancellationToken).ConfigureAwait(false);
        }
    }

    internal class HttpPutFormUrlEncoded : DefaultHttpWorkloadBase
    {
        readonly string url;
        readonly FormUrlEncodedContent body;

        public HttpPutFormUrlEncoded(string url, string jsonBody)
        {
            this.url = url;
            this.body = CreateFormUrlEncodedContent(jsonBody);
        }

        public override async Task ExecuteAsync(WorkloadContext context)
        {
            var response = await DefaultHttpClient.PutAsync(url, body, context.CancellationToken).ConfigureAwait(false);
            await ReadResponseAsync(response, context.CancellationToken).ConfigureAwait(false);
        }
    }
}
