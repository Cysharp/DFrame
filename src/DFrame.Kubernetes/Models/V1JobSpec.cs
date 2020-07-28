using System;
using System.Collections.Generic;
using System.Text;

namespace DFrame.Kubernetes.Models
{
    public class V1JobSpec
    {
        public long? activeDeadlineSeconds { get; set; }
        public int? backoffLimit { get; set; }
        public int? completions { get; set; }
        public bool? manualSelector { get; set; }
        public int? parallelism { get; set; }
        public V1LabelSelector selector { get; set; }
        public V1PodTemplateSpec template { get; set; }
        public int? ttlSecondsAfterFinished { get; set; }
    }
}
