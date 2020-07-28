using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;

namespace DFrame.Kubernetes.Responses
{
    public class HttpResponse : IDisposable
    {
        public HttpResponseMessage Response { get; set; }

        public void Dispose()
        {
            Response?.Content?.Dispose();
        }
    }

    public class HttpResponse<T> : HttpResponse
    {
        public T Body { get; }

        public HttpResponse(T body)
        {
            Body = body;
        }
    }
}
