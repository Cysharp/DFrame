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

memo for build & push.

```shell
docker build -t dframe_sample_k8s:0.1.0 -f sandbox/ConsoleAppK8s/Dockerfile .
docker tag dframe_sample_k8s:0.1.0 cysharp/dframe_sample_k8s
docker push cysharp/dframe_sample_k8s
```

rbac-less

```shell
kubectl apply -f sandbox/k8s/dframe/overlays/local/namespace.yaml
kubens dframe
kubectl delete secret aws-registry
kubectl create secret docker-registry aws-registry \
              --docker-server=https://<ACCOUNT_ID>.dkr.ecr.<REGION>.amazonaws.com \
              --docker-username=AWS \
              --docker-password=$(aws ecr get-login-password) \
              --docker-email=no@email.local
# kubectl delete deploy dframe-master
kubectl delete job dframe-master
kubectl kustomize sandbox/k8s/dframe/overlays/local | kubectl apply -f -
stern dframe*
```

rbac

```shell
kubectl kustomize sandbox/k8s/dframe/overlays/development | kubectl apply -f -
kubens dframe
stern dframe*

kubectl kustomize sandbox/k8s/dframe/overlays/development | kubectl delete -f -
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

### ab test on k8s

```
kubectl run -i --rm --restart=Never -n dframe --image=mocoso/apachebench apachebench -- bash -c "ab -n 10000 -c 10 http://77948c50-apiserver-apiserv-98d9-538745285.ap-northeast-1.elb.amazonaws.com/healthz"
```
