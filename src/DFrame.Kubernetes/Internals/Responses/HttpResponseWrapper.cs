using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;

namespace DFrame.Kubernetes.Internals.Responses
{
    internal readonly struct HttpResponseWrapper
    {
        public readonly HttpResponseMessage HttpResponseMessage;
        public readonly string Content;

        public HttpResponseWrapper(HttpResponseMessage httpResponseMessage, string content)
        {
            HttpResponseMessage = httpResponseMessage;
            Content = content;
        }

        public override bool Equals(object obj)
        {
            return base.Equals(obj);
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        public override string ToString()
        {
            return base.ToString();
        }
    }
}
