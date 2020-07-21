## Docker samples

### Out of Process Scaling Provider (oop)

try docker run.

```shell
docker run -it cysharp/dframe_sample_oop
```

memo for build & push.

```shell
docker build -t dframe_sample_oop:0.1.0 -f sandbox/ConsoleApp/Dockerfile .
docker tag dframe_sample_oop:0.1.0 cysharp/dframe_sample_oop
docker push cysharp/dframe_sample_oop
```

### Kubernetes Scaling Provider (k8s)

You can deploy DFrame to your Kubernetes cluster and run load test.
This sample contains Kustomize based kubernetes deployment.

If your cluster is rbac-less, like docker-desktop, following will work.

```shell
kubectl apply -f sandbox/k8s/dframe/overlays/local/namespace.yaml
kubens dframe
# Generate ImagePullSecret if you host image on your private registry like ECR.
kubectl delete secret aws-registry
kubectl create secret docker-registry aws-registry \
              --docker-server=https://<ACCOUNT_ID>.dkr.ecr.<REGION>.amazonaws.com \
              --docker-username=AWS \
              --docker-password=$(aws ecr get-login-password) \
              --docker-email=no@email.local
kubectl delete job dframe-master
kubectl kustomize sandbox/k8s/dframe/overlays/local | kubectl apply -f -
stern dframe*
```

If your cluster enabled rbac following will include ServiceAccount and Role.

```shell
kubectl kustomize sandbox/k8s/dframe/overlays/development | kubectl apply -f -
kubens dframe
stern dframe*

kubectl kustomize sandbox/k8s/dframe/overlays/development | kubectl delete -f -
```

If you already deployed service and rbac resources, service account and others, you can try fast load testing itelation by just change args and run  DFrame master as pod.
Below sample will run as 10 nodes, 10 workers, 1000 execute for ConsoleAppK8s.SampleHttpWorker scenario.

```shell
kubectl run -it --rm --restart=Never -n dframe --image=431046970529.dkr.ecr.ap-northeast-1.amazonaws.com/dframe-k8s:0.1.0 --image-pull-policy Always --env DFRAME_MASTER_HOST=dframe-master.dframe.svc.cluster.local --env DFRAME_WORKER_IMAGE_NAME=431046970529.dkr.ecr.ap-northeast-1.amazonaws.com/dframe-k8s --env DFRAME_WORKER_IMAGE_TAG="0.1.0" --serviceaccount='dframe-master' --port=12345 --labels="app=dframe-master" dframe-master -- "-nodeCount" "10" "-workerPerNode" "10" "-executePerWorker" "1000" "-scenarioName" "ConsoleAppK8s.SampleHttpWorker"
```


## etc....

### ab test on k8s

```shell
# 10並列 / 10000 リクエスト
kubectl run -i --rm --restart=Never -n dframe --image=mocoso/apachebench apachebench -- bash -c "ab -n 10000 -c 10 http://77948c50-apiserver-apiserv-98d9-538745285.ap-northeast-1.elb.amazonaws.com/healthz"
```

### deploy api server

let's launch apiserver to try httpclient access bench through dframe worker.

local

```shell
kubectl kustomize sandbox/k8s/apiserver/overlays/local | kubectl apply -f -
kubens apiserver
curl http://localhost:8080/api/weatherforecast
```

aws

```shell
kubectl kustomize sandbox/k8s/apiserver/overlays/aws | kubectl apply -f -
kubens apiserver
curl http://localhost:8080/api/weatherforecast

kubectl kustomize sandbox/k8s/apiserver/overlays/aws | kubectl delete -f -
```
