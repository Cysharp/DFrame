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
