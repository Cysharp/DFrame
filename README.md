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

Implement `Workload` class in your assembly.

```csharp
public class SampleHttpWorker : Workload
{
    const string url = "http://localhost:5000";

    HttpClient httpClient;

    public override async Task SetupAsync(WorkloadContext context)
    {
        var handler = new HttpClientHandler
        {
            MaxConnectionsPerServer = 100,
        };
        httpClient = new HttpClient(handler);
        httpClient.DefaultRequestHeaders.Add("ContentType", "application/json");
    }

    public override async Task ExecuteAsync(WorkloadContext context)
    {
        await httpClient.GetAsync(_url, cts.Token);
    }

    public override async Task TeardownAsync(WorkloadContext context)
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
  -workloadname <String>     (Required)
  -workercount <Int32>       (Default: 1)

Options:
  -workloadname <String>          (Required)
  -workercount <Int32>            (Required)
  -workloadperworker <Int32>      (Required)
  -executeperworkload <Int32>     (Required)

Options:
  -workloadname <String>            (Required)
  -workercount <Int32>              (Required)
  -maxworkloadperworker <Int32>     (Required)
  -workloadspawncount <Int32>       (Required)
  -workloadspawnsecond <Int32>      (Required)
```

* workerCount - scaling worker count. in kuberenetes means Pod count. for inprocess, recommend to use 1.
* workloadPerWorker - workload count of worker. This is similar to concurrent count.
* executePerWorkload - execute count per workload, if use for batch, recommend to set 1.
* workloadName - execute workload name, default is type name of Workload.

```
SampleApp.exe request -workercount 1 -workloadperworker 10 -executeperworkload 10 -workloadname SampleHttpWorkload
```

If use `RunDFrameLoadTestingAsync`, shows execution result like apache bench.

```test
Show Load Testing result report.
Finished 1 requests

Scaling Type:           InProcessScalingProvider
Workload Name:          SampleWorkload

Request count:          1
WorkerCount:            0
WorkloadPerWorker:      0
ExecutePerWorkload:     0
Concurrency level:      1
Complete requests:      1
Failed requests:        0

Time taken for tests:   0.05 seconds
Requests per seconds:   18.97 [#/sec] (mean)
Time per request:       52.73 [ms] (mean)
Time per request:       52.73 [ms] (mean, across all concurrent requests)

Percentage of the requests served within a certain time (ms)
 50%      52
 66%      52
 75%      52
 80%      52
 90%      52
 95%      52
 98%      52
 99%      52
100%      52 (longest request)
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

> Install-Package DFrame.Kubernetes  
> Install-Package DFrame.Ecs

License
---
This library is under the MIT License.
