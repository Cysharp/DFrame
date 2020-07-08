using System;
using System.Collections.Generic;
using System.Text;

namespace DFrame.KubernetesWorker
{
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
            return NormalizeNewLine(manifest);
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
            return NormalizeNewLine(manifest);
        }

        private static string NormalizeNewLine(string manifest)
        {
            return manifest.Replace("\r\n", "\n");
        }
    }
}
