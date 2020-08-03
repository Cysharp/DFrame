using System;
using System.IO;
using System.Runtime.Serialization;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using DFrame.Kubernetes.Exceptions;
using DFrame.Kubernetes.Models;
using DFrame.Kubernetes.Responses;
using DFrame.Kubernetes.Serializers;

namespace DFrame.Kubernetes
{
    public enum WatchEventType
    {
        /// <summary>
        /// When object created, modified existing, watch is first opened.
        /// </summary>
        [EnumMember(Value = "ADDED")] Added,
        /// <summary>
        /// When object modified
        /// </summary>
        [EnumMember(Value = "MODIFIED")] Modified,
        /// <summary>
        /// When object deleted
        /// </summary>
        [EnumMember(Value = "DELETED")] Deleted,
        /// <summary>
        /// When error happen while watching.
        /// common error 410 (Gone) means watch resource version is outdated, over 5min, and event lost, you need restart watch.
        /// </summary>
        [EnumMember(Value = "ERROR")] Error,
        /// <summary>
        /// When periodically to update the resource version.
        /// object contains only the resource version.
        /// </summary>
        [EnumMember(Value = "BOOKMARK")] Bookmark,
    }

    public class Watch<T> : IDisposable
    {
        private readonly Func<Task<TextReader>> _streamReaderFactory;
        private TextReader _streamReader;

        private readonly CancellationTokenSource _cts;

        /// <summary>
        /// Indicate currently watching
        /// </summary>
        public bool Watching { get; private set; }
        /// <summary>
        /// Event raise when kubernetes api server change resource T.
        /// </summary>
        public event Action<WatchEventType, T, CancellationTokenSource> OnEvent = delegate { };
        /// <summary>
        /// Event raise when exception happen during watch.
        /// </summary>
        public event Action<Exception> OnError = delegate { };
        /// <summary>
        /// Event raise when close watch connection to kubernetes api server.
        /// </summary>
        public event Action OnClosed = delegate { };

        public Watch(
            Func<Task<TextReader>> streamReaderFactory,
            Action<WatchEventType, T, CancellationTokenSource> onEvent, Action<Exception> onError = null, Action onClosed = null,
            CancellationTokenSource cts = default)
        {
            _streamReaderFactory = streamReaderFactory;
            OnEvent += onEvent;
            OnError += onError;
            OnClosed += onClosed;

            _cts = cts ?? new CancellationTokenSource();
        }

        public Watch(
            Func<Task<StreamReader>> streamReaderFactory,
            Action<WatchEventType, T, CancellationTokenSource> onEvent, Action<Exception> onError = null, Action onClosed = null,
            CancellationTokenSource cts = default)
            : this(async () => (TextReader)await streamReaderFactory().ConfigureAwait(false),
                    onEvent, onError, onClosed, cts)
        {
        }

        public void Dispose()
        {
            _cts?.Dispose();
            _streamReader?.Dispose();
        }

        public async Task Execute()
        {
            try
            {
                Watching = true;
                string line;
                _streamReader = await _streamReaderFactory().ConfigureAwait(false);

                // ReadLineAsync will return null when reached the end of the stream.
                while ((line = await _streamReader.ReadLineAsync().ConfigureAwait(false)) != null)
                {
                    if (_cts.IsCancellationRequested)
                        return;

                    try
                    {
                        var watchEvent = JsonConvert.DeserializeStringEnum<WatchEvent>(line);
                        if (watchEvent != null)
                        {
                            OnEvent?.Invoke(watchEvent.Type, watchEvent.Object, _cts);
                        }
                        else
                        {
                            var statusEvent = JsonConvert.DeserializeStringEnum<Watch<V1Status>.WatchEvent>(line);
                            var exception = new KubernetesException(statusEvent?.Object);
                            OnError?.Invoke(exception);
                        }
                    }
#pragma warning disable CA1031 // Do not catch general exception types
                    catch (Exception ex)
                    {
                        // deserialized failed or OnEvent throws
                        OnError?.Invoke(ex);
                    }
#pragma warning restore CA1031 // Do not catch general exception types
                }
            }
#pragma warning disable CA1031 // Do not catch general exception types
            catch (Exception ex)
            {
                OnError?.Invoke(ex);
            }
#pragma warning restore CA1031 // Do not catch general exception types
            finally
            {
                Watching = false;
                OnClosed?.Invoke();
            }
        }

        public class WatchEvent
        {
            public WatchEventType Type { get; set; }
            public T Object { get; set; }
        }
    }

    public static class WatchExtensions
    {
        public static Watch<T> Watch<T, TResponse>(this ValueTask<HttpResponse<TResponse>> responseTask,
            Action<WatchEventType, T, CancellationTokenSource> onEvent, Action<Exception> onError = null, Action onClosed = null,
            CancellationTokenSource cts = default)
        {
            return new Watch<T>(async () =>
            {
                var response = await responseTask.ConfigureAwait(false);
                if (!(response.Response.Content is Internals.WatcherDelegatingHandler.LineSeparatedHttpContent content))
                {
                    throw new KubernetesException("request is not watchable.");
                }
                return content.StreamReader;
            }, onEvent, onError, onClosed, cts);
        }

        public static Watch<T> Watch<T, TResponse>(this Task<HttpResponse<TResponse>> responseTask,
            Action<WatchEventType, T, CancellationTokenSource> onEvent, Action<Exception> onError = null, Action onClosed = null,
            CancellationTokenSource cts = default)
        {
            return new Watch<T>(async () =>
            {
                var response = await responseTask.ConfigureAwait(false);
                if (!(response.Response.Content is Internals.WatcherDelegatingHandler.LineSeparatedHttpContent content))
                {
                    throw new KubernetesException("request is not watchable.");
                }
                return content.StreamReader;
            }, onEvent, onError, onClosed, cts);
        }

        public static Watch<T> Watch<T, TResponse>(this HttpResponse<TResponse> response,
            Action<WatchEventType, T, CancellationTokenSource> onEvent, Action<Exception> onError = null, Action onClosed = null,
            CancellationTokenSource cts = default)
        {
            return Watch(Task.FromResult(response), onEvent, onError, onClosed, cts);
        }
    }
}
