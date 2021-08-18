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

To run on RBAC cluster, use following commands.

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

## Launch target server

Next, launch target HTTP and MagicOnion servers.

Deploy target server to EKS with following commands. Please install AWS LoadBalancer Controller to EKS.

```shell
kubectl kustomize sandbox/k8s/apiserver/overlays/aws | kubectl apply -f -
kubectl kustomize sandbox/k8s/magiconionserver/overlays/aws | kubectl apply -f -

HTTP_LB_ENDPOINT="http://$(kubectl get ingress apiserver -n apiserver -o jsonpath='{.items[].status.loadBalancer.ingress[].hostname}')"
GRPC_LB_ENDPOINT="http://$(kubectl get service magiconion -n apiserver -o jsonpath='{.status.loadBalancer.ingress[].hostname}'):12346"
```

Run below to ab HTTP server from local.

```shell
# confirm server is running and connectable
curl "${LB_ENDPOINT}"

# run ab test from pod. 10000 requests, 10 concurrency.
kubectl run -i --rm --restart=Never -n dframe --image=mocoso/apachebench apachebench -- bash -c "ab -n 10000 -c 10 ${HTTP_LB_ENDPOINT}"
```

## Load test target server

Faster load testing try-and-error is available by "change args" and "run DFrame master as pod".

Before deploy service and others before deploy dframe pod.

```shell
kubectl kustomize sandbox/k8s/dframe/overlays/fast-itelation | kubectl apply -f -
```

Next, replace bench target endpoint in ConsoleAppK8s/Program.cs, build and push image.

```shell
DOCKER_USER_NAME=<YOUR_NAME>
sed -e "s|<BENCH_HTTP_SERVER_HOST>|${HTTP_LB_ENDPOINT}|g" ./sandbox/ConsoleAppK8s/Program.cs -i
sed -e "s|<BENCH_GRPC_SERVER_HOST>|${GRPC_LB_ENDPOINT}|g" ./sandbox/ConsoleAppK8s/Program.cs -i
docker build -t ${DOCKER_USER_NAME}/dframe-k8s:0.1.0 -f ./sandbox/ConsoleAppK8s/Dockerfile .
docker push ${DOCKER_USER_NAME}/dframe-k8s:0.1.0
```

### SampleHttpWorker Scenario

This sample launch MagicOnion-Worker with `SampleHttpWorker` scenario and execute 1000000 requests, by `10 (process) * 10 (workers) * 10000 (execution)`.

```shell
kubectl run -it --rm --restart=Never -n dframe --image=${DOCKER_USER_NAME}/dframe-k8s:0.1.0 --image-pull-policy Always --env DFRAME_MASTER_CONNECT_TO_HOST=dframe-master.dframe.svc.cluster.local --env DFRAME_WORKER_IMAGE_NAME=${DOCKER_USER_NAME}/dframe-k8s --env DFRAME_WORKER_IMAGE_TAG="0.1.0" --env DFRAME_WORKER_IMAGE_PULL_POLICY="Always" --serviceaccount='dframe-master' --port=12345 --labels="app=dframe-master" dframe-master -- "batch -processCount" "10" "-workerPerProcess" "10" "-executePerWorker" "10000" "-workerName" "SampleHttpWorker"
```

### SampleUnaryWorker Scenario

Below sample will run 1000000 requests of SampleUnaryWorker, with 10 process, 10 workers and 10000 execution.

```shell
kubectl run -it --rm --restart=Never -n dframe --image=${DOCKER_USER_NAME}/dframe-k8s:0.1.0 --image-pull-policy Always --env DFRAME_MASTER_CONNECT_TO_HOST=dframe-master.dframe.svc.cluster.local --env DFRAME_WORKER_IMAGE_NAME=${DOCKER_USER_NAME}/dframe-k8s --env DFRAME_WORKER_IMAGE_TAG="0.1.0" --env DFRAME_WORKER_IMAGE_PULL_POLICY="Always" --serviceaccount='dframe-master' --port=12345 --labels="app=dframe-master" dframe-master -- "batch -processCount" "10" "-workerPerProcess" "10" "-executePerWorker" "10000" "-workerName" "SampleUnaryWorker"
```

### SampleStreamWorker Scenario

Below sample will run 1000000 requests of SampleStreamWorker, with 10 process, 10 workers and 10000 execution.

```shell
kubectl run -it --rm --restart=Never -n dframe --image=${DOCKER_USER_NAME}/dframe-k8s:0.1.0 --image-pull-policy Always --env DFRAME_MASTER_CONNECT_TO_HOST=dframe-master.dframe.svc.cluster.local --env DFRAME_WORKER_IMAGE_NAME=${DOCKER_USER_NAME}/dframe-k8s --env DFRAME_WORKER_IMAGE_TAG="0.1.0" --env DFRAME_WORKER_IMAGE_PULL_POLICY="Always" --serviceaccount='dframe-master' --port=12345 --labels="app=dframe-master" dframe-master -- "batch -processCount" "10" "-workerPerProcess" "10" "-executePerWorker" "10000" "-workerName" "SampleStreamWorker"
```

# ECS

You can deploy DFrame to your ECS cluster and run load test via ECS Scaling Provider (DFrame.Ecs).
This sample contains AWS CDK based ECS deployment.

**Prerequisites**

Following commands are used on this sample.

* [aws-cdk](https://github.com/aws/aws-cdk)

Install cdk cli.

```shell
npm install -g aws-cdk
npm update -g aws-cdk
```

## First step samples

This samples confirm DFrame Master and Workers are successfully communicating.
Before trying benchmark to external Server, run this sample.

To run on ECS, use following commands.

```shell
cd sandbox/Ecs
cdk synth
cdk deploy
```

After deployment complete, check ECS logs DFrameWorker communicating with DFrameMaster.

> DFrameWorker Logs: https://ap-northeast-1.console.aws.amazon.com/ecs/home?region=ap-northeast-1#/clusters/DFrameCdkStack-Cluster/services/DFrameWorkerService/logs

## Load test target server

### SampleHttpWorker Scenario

Below command will launch 10 worker fargate with `SampleHttpWorker` scenario.

```shell
cdk deploy -c "dframeArg=request -processCount 10 -workerPerProcess 1 -executePerWorker 1 -workerName SampleHttpWorker"
```

After deployment complete, check ECS logs EchoServer communicating with DFrameMaster.

> EchoServer Logs: https://ap-northeast-1.console.aws.amazon.com/ecs/home?region=ap-northeast-1#/clusters/DFrameCdkStack-Cluster/services/EchoServer/logs

### SampleUnaryWorker Scenario

Below command will launch 10 worker fargate with `SampleUnaryWorker` scenario.

```shell
cdk deploy -c "dframeArg=request -processCount 10 -workerPerProcess 1 -executePerWorker 1 -workerName SampleUnaryWorker"
```

> MagicOnionServer Logs: https://ap-northeast-1.console.aws.amazon.com/ecs/home?region=ap-northeast-1#/clusters/DFrameCdkStack-Cluster/services/MagicOnionServer/logs

### SampleStreamWorker Scenario

Below command will launch 10 worker fargate with `SampleStreamWorker` scenario.

```shell
cdk deploy -c "dframeArg=request -processCount 10 -workerPerProcess 1 -executePerWorker 1 -workerName SampleStreamWorker"
```

> MagicOnionServer Logs: https://ap-northeast-1.console.aws.amazon.com/ecs/home?region=ap-northeast-1#/clusters/DFrameCdkStack-Cluster/services/MagicOnionServer/logs

## TIPS

**Q. How to add Datadog sidecar.**

CDK uses AWS SecretsManager to keep datadog token.
Once CDK Deployed, create datadog token secret with secret-id `dframe-datadog-token`.

```shell
# via aws cli
SECRET_ID=dframe-datadog-token
DD_TOKEN=abcdefg12345 # replace with your datadog token
aws secretsmanager create-secret --name "$SECRET_ID"
aws secretsmanager put-secret-value --secret-id "$SECRET_ID" --secret-string "${DD_TOKEN}"
```

Confirm token is successfully set to secrets manager.

```shell
aws secretsmanager describe-secret --secret-id "$SECRET_ID"
aws secretsmanager get-secret-value --secret-id "$SECRET_ID"
```

To add Datadog agent sidecar, set `UseFargateDatadogAgentProfiler = true`, then CDK Deploy again.

```csharp
new ReportStackProps
{
    UseFargateDatadogAgentProfiler = true, // Add datadog agent sidecar to your fargate containers.
}
```
