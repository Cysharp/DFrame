using System.Collections.Generic;

namespace DFrame.Kubernetes.Models
{
    public class V1StatusDetails
    {
        public IList<V1StatusCause> causes { get; set; }
        public string group { get; set; }
        public string kind { get; set; }
        public string name { get; set; }
        public int? retryAfterSeconds { get; set; }
        public string uid { get; set; }
    }
}