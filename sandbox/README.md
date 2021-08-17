# Getting Started

Use ConsoleApp to try DFrame with InProcessScalingProvider and LoadTest to HTTP(S) Server.

**Visual Studio**

Open DFrame.sln and launch EchoServer then ConsoleApp.

> If you are using [SwitchStartupProject for VS2019](https://heptapod.host/thirteen/switchstartupproject) use `ConsoleApp + EchoServer`.

**dotnet cli**

run echo server.

```shell
docker run -it --rm -p 5000:80 cysharp/dframe-echoserver:latest
```

run sample ConsoleApp.

```shell
dotnet run --project sandbox/ConsoleApp
```

# WebApp

WebApp provide WebUI and record Profiler History to database.
Use EntityFramework to use database.

```shell
dotnet new tool-manifest
dotnet tool install dotnet-ef
```

add migrations.

```shell
dotnet ef migrations add docker
```

run migrations.

```shell
docker-compose -f sandbox/docker-compose.yaml up
```

# docker

Try inprocess or Out of Process (oop).

```shell
docker run -it cysharp/dframe_sample_oop
```

memo for build & push.

```shell
docker build -t dframe_sample_oop:0.1.0 -f sandbox/ConsoleApp/Dockerfile .
docker tag dframe_sample_oop:0.1.0 cysharp/dframe_sample_oop
docker push cysharp/dframe_sample_oop
```

# Kubernetes

You can deploy DFrame to your Kubernetes cluster and run load test via Kubernetes Scaling Provider (DFrame.Kubernetes).
This sample contains Kustomize based kubernetes deployment.

**Prerequisites**

Following commands are used on this sample.

* [kubens](https://github.com/ahmetb/kubectx)
* [stern](https://github.com/wercker/stern)

## First step samples

These samples confirm DFrame Master and Workers are successfully communicating.
Before trying benchmark to external Server, run one of this sample.

### Sample1. Deploy to RBAC-less Kubernetes

To run on RBAC-less cluster, run following commands from repository root. RBAC-less sample can be used for Kubernetes running on Docker for Windows/macOS.

```shell
# ECR Info
ACCOUNT_ID=431046970529 # your aws account id
REGION=ap-northeast-1 # your aws region

# Prepare namespace and switch to
kubectl apply -f sandbox/k8s/dframe/overlays/rbacless/namespace.yaml
kubens dframe
# Generate ImagePullSecret if you host image on your private registry like ECR.
kubectl delete secret aws-registry
kubectl create secret docker-registry aws-registry \
              --docker-server=https://${ACCOUNT_ID}.dkr.ecr.${REGION}.amazonaws.com \
              --docker-username=AWS \
              --docker-password=$(aws ecr get-login-password) \
              --docker-email=no@email.local
# Deploy
kubectl delete job dframe-master
kubectl kustomize sandbox/k8s/dframe/overlays/rbacless | kubectl apply -f -
# Check logs
stern dframe*
```

After execution, clean up resources by following command.

```shell
# clean up
kubectl kustomize sandbox/k8s/dframe/overlays/rbacless | kubectl delete -f -
```

### Sample2. Deploy to RBAC Kubernetes

To run on RBAC cluster, use following commands. This enable ServiceAccount and Roles.

```shell
# Deploy
kubectl kustomize sandbox/k8s/dframe/overlays/development | kubectl apply -f -
# Check logs
stern dframe* -n dframe
```

After execution, clean up resources by following command.

```shell
kubectl kustomize sandbox/k8s/dframe/overlays/development | kubectl delete -f -
```

### Sample3. Deploy to EKS NodeGroup

To run on EKS NodeGroup named `dframe`, use following commands.

```shell
# Deploy
kubectl kustomize sandbox/k8s/dframe/overlays/eks-nodegroup | kubectl apply -f -
# Check logs
stern dframe* -n dframe
```

After execution, clean up resources by following command.

```shell
kubectl kustomize sandbox/k8s/dframe/overlays/eks-nodegroup | kubectl delete -f -
```

### Sample4. Deploy to EKS Fargate

To run on EKS Fargate, use following commands. Fargate profile is enable to `dframe-fargate` namespace.

> NOTE: Make sure Fargate pod is slow to start, it takes 30sec to 150sec until pod become Ready state. You may need wait until Fargates start your DFrame Worker pods.

```shell
kubectl kustomize sandbox/k8s/dframe/overlays/eks-fargate | kubectl apply -f -
stern dframe* -n dframe-fargate
```

After execution, clean up resources by following command.

```shell
kubectl kustomize sandbox/k8s/dframe/overlays/eks-fargate | kubectl delete -f -
```

> TIPS: If you need confirm fargate profile is collect, here's commandline sample to run dframe-master.

```shell
ACCOUNT_ID=431046970529 # your aws account id
REGION=ap-northeast-1 # your aws region

kubectl run -it --rm --restart=Never -n dframe-fargate --image=${ACCOUNT_ID}.dkr.ecr.${REGION}.amazonaws.com/dframe-k8s:0.1.0 --image-pull-policy Always --env DFRAME_MASTER_CONNECT_TO_HOST=dframe-master.dframe-fargate.svc.cluster.local --env DFRAME_WORKER_IMAGE_NAME=${ACCOUNT_ID}.dkr.ecr.${REGION}.amazonaws.com/dframe-k8s --env DFRAME_WORKER_IMAGE_TAG="0.1.0" --env DFRAME_WORKER_IMAGE_PULL_POLICY="Always" --serviceaccount='dframe-master' --port=12345 --labels="app=dframe-master" dframe-master -- "request" "-processCount" "1" "-workerPerProcess" "10" "-executePerWorker" "10" "-workerName" "SampleWorker"
```

## (Advanced) Step1. Launch loadtest target server

Now you are ready to benchmark external server.
Let's launch target server and benchmark to it.

### Choice 1. Launch Http Echo Server

Let's launch API-Server to serve dframe worker HttpClient benchmark.

This server accept sandbox DFrame `ConsoleApp.SampleHttpWorker` scenario requests.

**Run on local**

```shell
kubectl kustomize sandbox/k8s/apiserver/overlays/local | kubectl apply -f -
```

Below sample will run ab test.

```shell
# confirm server is running and connectable
curl "http://localhost:8080"

# 10000 requests, 10 concurrency.
kubectl run -i --rm --restart=Never -n dframe --image=mocoso/apachebench apachebench -- bash -c "ab -n 10000 -c 10 http://apiserver:8080"
```

**Run on aws**

```shell
kubectl kustomize sandbox/k8s/apiserver/overlays/aws | kubectl apply -f -
```

Below sample will run ab test.

```shell
LB_ENDPOINT="http://$(kubectl get ingress apiserver -o jsonpath='{.items[].status.loadBalancer.ingress[].hostname}')/healthz"

# confirm server is running and connectable
curl "${LB_ENDPOINT}"

# 10000 requests, 10 concurrency.
kubectl run -i --rm --restart=Never -n dframe --image=mocoso/apachebench apachebench -- bash -c "ab -n 10000 -c 10 ${LB_ENDPOINT}"
```

### Choice2. Use MagicOnion Echo Server

let's launch MagicOnion to serve dframe worker gRPC benchmark.

This server accept sandbox DFrame `ConsoleApp.SampleUnaryWorker` scenario and `ConsoleApp.SampleStreamWorker` scenario requests.

**Run on local**

```shell
kubectl kustomize sandbox/k8s/apiserver/overlays/local | kubectl apply -f -
# confirm server is running and connectable
echo localhost:12346
```

**Run on aws**

```shell
kubectl kustomize sandbox/k8s/magiconionserver/overlays/aws | kubectl apply -f -
kubens apiserver
echo "$(kubectl get service magiconion -o jsonpath='{.status.loadBalancer.ingress[].hostname}'):12346"
```

## (Advanced) Step2. Launch DFrame Master and loadtest target

Faster load testing itelation is available by "change args" and "run DFrame master as pod".

Before trying run dframe, deploy service and and others.

```shell
kubectl kustomize sandbox/k8s/dframe/overlays/fast-telation | kubectl apply -f -
```

You are ready to run ondemand pod.

### Run SampleHttpWorker Scenario

Below sample will run 1000000 requests of SampleHttpWorker, with 10 process, 10 workers and 10000 execution.

```shell
ACCOUNT_ID=431046970529 # your aws account id
REGION=ap-northeast-1 # your aws region

kubectl run -it --rm --restart=Never -n dframe --image=${ACCOUNT_ID}.dkr.ecr.${REGION}.amazonaws.com/dframe-k8s:0.1.0 --image-pull-policy Always --env DFRAME_MASTER_CONNECT_TO_HOST=dframe-master.dframe.svc.cluster.local --env DFRAME_WORKER_IMAGE_NAME=${ACCOUNT_ID}.dkr.ecr.${REGION}.amazonaws.com/dframe-k8s --env DFRAME_WORKER_IMAGE_TAG="0.1.0" --env DFRAME_WORKER_IMAGE_PULL_POLICY="Always" --serviceaccount='dframe-master' --port=12345 --labels="app=dframe-master" dframe-master -- "batch -processCount" "10" "-workerPerProcess" "10" "-executePerWorker" "10000" "-workerName" "SampleHttpWorker"
```

### Run SampleUnaryWorker Scenario

Below sample will run 1000000 requests of SampleUnaryWorker, with 10 process, 10 workers and 10000 execution.

```shell
ACCOUNT_ID=431046970529 # your aws account id
REGION=ap-northeast-1 # your aws region

kubectl run -it --rm --restart=Never -n dframe --image=${ACCOUNT_ID}.dkr.ecr.${REGION}.amazonaws.com/dframe-k8s:0.1.0 --image-pull-policy Always --env DFRAME_MASTER_CONNECT_TO_HOST=dframe-master.dframe.svc.cluster.local --env DFRAME_WORKER_IMAGE_NAME=${ACCOUNT_ID}.dkr.ecr.${REGION}.amazonaws.com/dframe-k8s --env DFRAME_WORKER_IMAGE_TAG="0.1.0" --env DFRAME_WORKER_IMAGE_PULL_POLICY="Always" --serviceaccount='dframe-master' --port=12345 --labels="app=dframe-master" dframe-master -- "batch -processCount" "10" "-workerPerProcess" "10" "-executePerWorker" "10000" "-workerName" "SampleUnaryWorker"
```

### Run SampleStreamWorker Scenario

Below sample will run 1000000 requests of SampleStreamWorker, with 10 process, 10 workers and 10000 execution.

```shell
ACCOUNT_ID=431046970529 # your aws account id
REGION=ap-northeast-1 # your aws region

kubectl run -it --rm --restart=Never -n dframe --image=${ACCOUNT_ID}.dkr.ecr.${REGION}.amazonaws.com/dframe-k8s:0.1.0 --image-pull-policy Always --env DFRAME_MASTER_CONNECT_TO_HOST=dframe-master.dframe.svc.cluster.local --env DFRAME_WORKER_IMAGE_NAME=${ACCOUNT_ID}.dkr.ecr.${REGION}.amazonaws.com/dframe-k8s --env DFRAME_WORKER_IMAGE_TAG="0.1.0" --env DFRAME_WORKER_IMAGE_PULL_POLICY="Always" --serviceaccount='dframe-master' --port=12345 --labels="app=dframe-master" dframe-master -- "batch -processCount" "10" "-workerPerProcess" "10" "-executePerWorker" "10000" "-workerName" "SampleStreamWorker"
```
