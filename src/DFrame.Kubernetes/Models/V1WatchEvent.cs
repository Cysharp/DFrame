namespace DFrame.Kubernetes.Models
{
    public class V1WatchEvent<T>
    {
        public T @object { get; set; }
        public string type { get; set; }
    }
}
