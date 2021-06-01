DFrame
===
Kubernetes based Micro **D**istributed Batch **Frame**work and Load Testing Library for C#.

This library allows you to write distributed batch or load test scenarios in C#. In addition to HTTP/1, you can test HTTP/2, gRPC, MagicOnion, Photon, or original network transport by writing in C#.

**Work In Progress** Preview `0.0.4`.

<!-- START doctoc generated TOC please keep comment here to allow auto update -->
<!-- DON'T EDIT THIS SECTION, INSTEAD RE-RUN doctoc TO UPDATE -->
## Table of Contents

- [Getting started](#getting-started)
- [Distributed Collections](#distributed-collections)
- [Kubernetes](#kubernetes)
- [License](#license)

<!-- END doctoc generated TOC please keep comment here to allow auto update -->

Getting started
---
Install via NuGet

> Install-Package DFrame  
> Install-Package DFrame.LoadTesting  

Sample code of HTTP/1 load testing.

Implement `Worker` class in your assembly.

```csharp
public class SampleHttpWorker : Worker
{
    const string url = "http://localhost:5000";

    HttpClient httpClient;

    public override async Task SetupAsync(WorkerContext context)
    {
        var handler = new HttpClientHandler
        {
            MaxConnectionsPerServer = 100,
        };
        httpClient = new HttpClient(handler);
        httpClient.DefaultRequestHeaders.Add("ContentType", "application/json");
    }

    public override async Task ExecuteAsync(WorkerContext context)
    {
        await httpClient.GetAsync(_url, cts.Token);
    }

    public override async Task TeardownAsync(WorkerContext context)
    {
    }
}
```

Setup entrypoint `RunDFrameAsync` or `RunDFrameLoadTestingAsync`.

```csharp
class Program
{
    static async Task Main(string[] args)
    {
        await Host.CreateDefaultBuilder(args)
            // .RunDFrameAsync(args, new DFrameOptions("localhost", 12345));
            .RunDFrameLoadTestingAsync(args, new DFrameOptions("localhost", 12345));
    }
```

And execute commandline.

```
Usage: <Command> <Args>

Commands:
  batch
  request
  rampup
```

```
batch Options:
  -workerName <String>      (Required)
  -processCount <Int32>     (Default: 1)

request Options:
  -workerName <String>          (Required)
  -processCount <Int32>         (Required)
  -workerPerProcess <Int32>     (Required)
  -executePerWorker <Int32>     (Required)

rampup Options:
  -workerName <String>             (Required)
  -processCount <Int32>            (Required)
  -maxWorkerPerProcess <Int32>     (Required)
  -workerSpawnCount <Int32>        (Required)
  -workerSpawnSecond <Int32>       (Required)
```

* processCount - scaling process count. in kuberenetes means Pod count. for inprocess, recommend to use 1.
* workerPerProcess - worker count of process. This is similar to concurrent count.
* executePerWorker - execute count per worker, if use for batch, recommend to set 1.
* workerName - execute worker name, default is type name of Worker.

```
SampleApp.exe "reqeust -processCount 1 -workerPerProcess 10 -executePerWorker 1000 -workerName "SampleHttpWorker"
```

If use `RunDFrameLoadTestingAsync`, shows execution result like apache bench.

```test
Show Load Testing result report.
Finished 10000 requests
Scaling Type:           InProcessScalingProvider
Request count:          10000
ProcessCount:              1
WorkerPerProcess:          10
ExecutePerWorker:       1000
Concurrency level:      10
Complete requests:      10000
Failed requests:        0
Time taken for tests:   19.15 seconds
Requests per seconds:   522.16 [#/sec] (mean)
Time per request:       19.15 [ms] (mean)
Time per request:       1.92 [ms] (mean, across all concurrent requests)
Percentage of the requests served within a certain time (ms)
 50%      18
 66%      19
 75%      19
 80%      20
 90%      20
 95%      20
 98%      21
 99%      22
100%      378 (longest request)
```

Distributed Collections
---
Data can be shared between workers via DistirbutedColleciton.

* `DistributedList<T>`
* `DistributedQueue<T>`
* `DistributedStack<T>`
* `DistributedHashSet<T>`
* `DistributedDictionary<TKey, TValue>`

```csharp
public class SampleWorker : Worker
{
    IDistributedQueue<int> queue;
    Random rand;

    public override async Task SetupAsync(WorkerContext context)
    {
        queue = context.CreateDistributedQueue<int>("sampleworker-testq");
        rand = new Random();
    }

    public override async Task ExecuteAsync(WorkerContext context)
    {
        await queue.EnqueueAsync(rand.Next());
    }

    public override async Task TeardownAsync(WorkerContext context)
    {
        while (true)
        {
            var v = await queue.TryDequeueAsync();
            if (v.HasValue)
            {
                Console.WriteLine($"Dequeue all from {Environment.MachineName} {context.WorkerId}: {v.Value}");
            }
            else
            {
                return;
            }
        }
    }
}
```

Kubernetes
---
WIP, DFrame scales 1-10000 workers via Kuberenetes. Distributed batches can be written collaboratively through inter-worker communication through Distributed Collections. It also enables large scale load testing.

You can choose for Kuberentes or AWS ECS(includes Fargate)

> Install-Package DFrame.Kuberentes  
> Install-Package DFrame.Ecs

License
---
This library is under the MIT License.
