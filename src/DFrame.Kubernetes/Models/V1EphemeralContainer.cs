using System.Collections.Generic;

namespace DFrame.Kubernetes.Models
{
    public class V1EphemeralContainer
    {
        public IList<string> Args { get; set; }
        public IList<string> Command { get; set; }
        public IList<V1EnvVar> Env { get; set; }
        public IList<V1EnvFromSource> EnvFrom { get; set; }
        public string Image { get; set; }
        public string ImagePullPolicy { get; set; }
        public V1Lifecycle Lifecycle { get; set; }
        public V1Probe LivenessProbe { get; set; }
        public string Name { get; set; }
        public IList<V1ContainerPort> Ports { get; set; }
        public V1Probe ReadinessProbe { get; set; }
        public V1ResourceRequirements Resources { get; set; }
        public V1SecurityContext SecurityContext { get; set; }
        public V1Probe StartupProbe { get; set; }
        public bool? Stdin { get; set; }
        public bool? StdinOnce { get; set; }
        public string TargetContainerName { get; set; }
        public string TerminationMessagePath { get; set; }
        public string TerminationMessagePolicy { get; set; }
        public bool? Tty { get; set; }
        public IList<V1VolumeDevice> VolumeDevices { get; set; }
        public IList<V1VolumeMount> VolumeMounts { get; set; }
        public string WorkingDir { get; set; }
    }
}
