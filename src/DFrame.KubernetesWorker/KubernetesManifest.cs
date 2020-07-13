using System;
using System.Collections.Generic;
using System.Text;

namespace DFrame.KubernetesWorker
{
    // may use T4 to gen manifests.
    public class KubernetesManifest
    {
        public static string GetNamespace(string name)
        {
            var manifest = $@"---
apiVersion: v1
kind: Namespace
metadata:
  name: {name}
";
            return LinuxNewLine(manifest);
        }

        public static string GetJob(string name, string image, string imageTag, string host, string imagePullPolicy = "IfNotPresent", string imagePullSecret = "", int parallelism = 1)
        {
            // todo: resources and limits should be same value to avoid pod move on resource exhaust
            var manifest = $@"---
apiVersion: batch/v1
kind: Job
metadata:
  name: {name}
  labels:
    app: {name}
spec:
  parallelism: {parallelism}
  completions: {parallelism}
  template:
    metadata:
      labels:
        app: {name}
    spec:
      restartPolicy: Never
      containers:
        - name: {name}
          image: {image}:{imageTag}
          imagePullPolicy: {imagePullPolicy}
          args: [""--worker-flag""]
          env:
            - name: DFRAME_MASTER_HOST
              value: ""{host}""
          resources:
            requests:
              cpu: 100m
              memory: 100Mi
            limits:
              cpu: 2000m
              memory: 1000Mi
      {(imagePullSecret != "" ? $@"imagePullSecrets:
        - name: {imagePullSecret}" : "")}
";
            return LinuxNewLine(manifest);
        }

        public static string GetDeployment(string name, string image, string imageTag, string host, string imagePullPolicy = "IfNotPresent", string imagePullSecret = "", int replicas = 1)
        {
            var manifest = $@"---
apiVersion: apps/v1
kind: Deployment
metadata:
  name: {name}
  labels:
    app: {name}
spec:
  replicas: {replicas}
  selector:
    matchLabels:
      app: {name}
  template:
    metadata:
      labels:
        app: {name}
    spec:
      containers:
        - name: {name}
          image: {image}:{imageTag}
          imagePullPolicy: {imagePullPolicy}
          args: [""--worker-flag""]
          env:
            - name: DFRAME_MASTER_HOST
              value: ""{host}""
          resources:
            requests:
              cpu: 100m
              memory: 100Mi
            limits:
              cpu: 2000m
              memory: 1000Mi
      {(imagePullSecret != "" ? $@"imagePullSecrets:
        - name: {imagePullSecret}" : "")}
";
            return LinuxNewLine(manifest);
        }

        private static string LinuxNewLine(string manifest)
        {
            return manifest.Replace("\r\n", "\n");
        }
    }
}
