using System;
using System.Collections.Generic;
using System.Text;

namespace DFrame.Kubernetes.Models
{
    public class V1JobStatus
    {
        public int? Active { get; set; }
        public DateTime? CompletionTime { get; set; }
        public IList<V1JobCondition> Conditions { get; set; }
        public int? Failed { get; set; }
        public DateTime? StartTime { get; set; }
        public int? Succeeded { get; set; }
    }
}
