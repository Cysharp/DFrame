docker build and push

```shell
docker build -t cysharp/dframe-magiconion:0.0.1 -f sandbox/EchoMagicOnion/Dockerfile .
docker tag cysharp/dframe-magiconion:0.0.1 cysharp/dframe-magiconion:latest
docker push cysharp/dframe-magiconion:0.0.1
docker push cysharp/dframe-magiconion:latest
```