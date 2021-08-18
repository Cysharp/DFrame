docker build and push

```shell
docker build -t cysharp/dframe-magiconion:0.0.4 -f sandbox/EchoMagicOnion/Dockerfile .
docker tag cysharp/dframe-magiconion:0.0.4 cysharp/dframe-magiconion:latest
docker push cysharp/dframe-magiconion:0.0.4
docker push cysharp/dframe-magiconion:latest
```