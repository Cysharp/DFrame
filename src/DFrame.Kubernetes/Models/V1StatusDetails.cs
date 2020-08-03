using System.Collections.Generic;

namespace DFrame.Kubernetes.Models
{
    public class V1StatusDetails
    {
        public IList<V1StatusCause> Causes { get; set; }
        public string Group { get; set; }
        public string Kind { get; set; }
        public string Name { get; set; }
        public int? RetryAfterSeconds { get; set; }
        public string Uid { get; set; }
    }
}