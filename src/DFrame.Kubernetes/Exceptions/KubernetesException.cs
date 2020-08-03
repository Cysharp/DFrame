using System;
using DFrame.Kubernetes.Models;

namespace DFrame.Kubernetes.Exceptions
{
    public class KubernetesException : Exception
    {
        public V1Status Status { get; private set; }

        public KubernetesException()
        {
        }
        public KubernetesException(V1Status status)
            : this(status?.Message)
        {
            this.Status = status;
        }

        public KubernetesException(string message)
            : base(message)
        {
        }

        public KubernetesException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}
