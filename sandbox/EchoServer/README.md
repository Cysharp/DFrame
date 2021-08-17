Build docker image and push.

```shell
docker build -t cysharp/dframe-echoserver:0.0.1 -f sandbox/EchoServer/Dockerfile .
docker tag cysharp/dframe-echoserver:0.0.1 cysharp/dframe-echoserver:latest
docker push cysharp/dframe-echoserver:0.0.1
docker push cysharp/dframe-echoserver:latest
```
