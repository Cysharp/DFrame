Build docker image and push.

```shell
docker build -t cysharp/dframe-echoserver:0.0.4 -f sandbox/EchoServer/Dockerfile .
docker tag cysharp/dframe-echoserver:0.0.4 cysharp/dframe-echoserver:latest
docker push cysharp/dframe-echoserver:0.0.4
docker push cysharp/dframe-echoserver:latest
```
