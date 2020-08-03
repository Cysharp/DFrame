using System;
using System.Collections.Generic;
using System.Text;

namespace DFrame.Kubernetes.Models
{
    public class V1JobSpec
    {
        public long? ActiveDeadlineSeconds { get; set; }
        public int? BackoffLimit { get; set; }
        public int? Completions { get; set; }
        public bool? ManualSelector { get; set; }
        public int? Parallelism { get; set; }
        public V1LabelSelector Selector { get; set; }
        public V1PodTemplateSpec Template { get; set; }
        public int? TtlSecondsAfterFinished { get; set; }
    }
}
