using System;
using System.Collections.Generic;
using System.Text;

namespace DFrame.Kubernetes.Models
{
    public class V1JobStatus
    {
        public int? active { get; set; }
        public DateTime? completionTime { get; set; }
        public IList<V1JobCondition> conditions { get; set; }
        public int? failed { get; set; }
        public DateTime? startTime { get; set; }
        public int? succeeded { get; set; }
    }
}
