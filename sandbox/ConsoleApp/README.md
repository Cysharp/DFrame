docker build -t dframe-worker:0.1.0 -f sandbox/ConsoleApp/Dockerfile .
docker tag dframe-worker:0.1.0 cysharp/dframe-worker
docker push cysharp/dframe-worker