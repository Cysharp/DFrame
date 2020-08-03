namespace DFrame.Kubernetes.Models
{
    public class V1VolumeMount
    {
        public string MountPath { get; set; }
        public string MountPropagation { get; set; }
        public string Name { get; set; }
        public bool ReadOnly { get; set; }
        public string SubPath { get; set; }
        public string SubPathExpr { get; set; }
    }
}
