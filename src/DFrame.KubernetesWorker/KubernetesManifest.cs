using System;
using System.Collections.Generic;
using System.Text;

namespace DFrame.KubernetesWorker
{
    public class KubernetesManifest
    {
        public static string GetNamespace(string name)
        {
            return $@"---
apiVersion: v1
kind: Namespace
metadata:
  name: {name}
";
        }
        public static string GetDeployment(string name, string image, string imageTag, string host, int replicas = 1)
        {
            return $@"---
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
";
        }
    }
}
