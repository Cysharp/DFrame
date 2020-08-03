namespace DFrame.Kubernetes.Models
{
    public class V1EnvVarSource
    {
        public V1ConfigMapKeySelector ConfigMapKeyRef { get; set; }
        public V1ObjectFieldSelector FieldRef { get; set; }
        public V1ResourceFieldSelector ResourceFieldRef { get; set; }
        public V1SecretKeySelector SecretKeyRef { get; set; }
    }
}
