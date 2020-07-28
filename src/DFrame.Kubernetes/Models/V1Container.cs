using System.Collections.Generic;

namespace DFrame.Kubernetes.Models
{
    public class V1Container
    {
        public IList<string> args { get; set; }
        public IList<string> command { get; set; }
        public IList<V1EnvVar> env { get; set; }
        public IList<V1EnvFromSource> envFrom { get; set; }
        public string image { get; set; }
        public string imagePullPolicy { get; set; }
        public V1Lifecycle lifecycle { get; set; }
        public V1Probe livenessProbe { get; set; }
        public string name { get; set; }
        public IList<V1ContainerPort> ports { get; set; }
        public V1Probe readinessProbe { get; set; }
        public V1ResourceRequirements resources { get; set; }
        public V1SecurityContext securityContext { get; set; }
        public V1Probe startupProbe { get; set; }
        public bool stdin { get; set; }
        public bool stdinOnce { get; set; }
        public string terminationMessagePath { get; set; }
        public string terminationMessagePolicy { get; set; }
        public bool? tty { get; set; }
        public IList<V1VolumeDevice> volumeDevices { get; set; }
        public IList<V1VolumeMount> volumeMounts { get; set; }
        public string workingDir { get; set; }
    }
}
