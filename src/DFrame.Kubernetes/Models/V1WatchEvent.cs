namespace DFrame.Kubernetes.Models
{
    public class V1WatchEvent<T>
    {
        public T Object { get; set; }
        public string Type { get; set; }
    }
}
