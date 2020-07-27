namespace DFrame.KubernetesWorker.Models
{
    public class V1VolumeMount
    {
        public string mountPath { get; set; }
        public string mountPropagation { get; set; }
        public string name { get; set; }
        public bool readOnly { get; set; }
        public string subPath { get; set; }
        public string subPathExpr { get; set; }
    }
}
