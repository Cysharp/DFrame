namespace DFrame.KubernetesWorker.Models
{
    public class V1EnvVarSource
    {
        public V1ConfigMapKeySelector configMapKeyRef { get; set; }
        public V1ObjectFieldSelector fieldRef { get; set; }
        public V1ResourceFieldSelector resourceFieldRef { get; set; }
        public V1SecretKeySelector secretKeyRef { get; set; }
    }
}
