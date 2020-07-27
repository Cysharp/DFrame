using System;

namespace DFrame.KubernetesWorker.Models
{
    public class V1ManagedFieldsEntry
    {
        public string apiVersion { get; set; }
        public string fieldsType { get; set; }
        public object fieldsV1 { get; set; }
        public string manager { get; set; }
        public string operation { get; set; }
        public DateTime? time { get; set; }
    }
}
