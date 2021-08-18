Build docker image and push.

```shell
docker build -t cysharp/dframe-consoleappecs:0.0.4 -f sandbox/ConsoleAppEcs/Dockerfile .
docker tag cysharp/dframe-consoleappecs:0.0.4 cysharp/dframe-consoleappecs:latest
docker push cysharp/dframe-consoleappecs:0.0.4
docker push cysharp/dframe-consoleappecs:latest
```
