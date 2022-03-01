# DFrame
[![GitHub Actions](https://github.com/Cysharp/DFrame/workflows/Build-Debug/badge.svg)](https://github.com/Cysharp/DFrame/actions) [![Releases](https://img.shields.io/github/release/Cysharp/DFrame.svg)](https://github.com/Cysharp/DFrame/releases)

**D**istributed load-testing **Frame**work for .NET and Unity.

This library allows you to write distributed load test scenarios in plain C#. In addition to HTTP/1, you can test HTTP/2, gRPC, MagicOnion, Photon, or original network transport by writing in C#.

![dframe](https://user-images.githubusercontent.com/46207/155902346-8dc6459f-d545-4557-854b-0920f0b36c07.gif)

DFrame is similar as [Locust](https://locust.io/), combination of two parts, `DFrame.Controller`(built by Blazor Server) as Web UI and `DFrame.Worker` as C# test scenario script. DFrame is providing as a library however you can bootup easily if you are faimiliar with C#.

```csharp
// Install-Package DFrame
using DFrame;

DFrameApp.Run(7312, 7313); // WebUI:7312, WorkerListen:7313

public class SampleWorkload : Workload
{
    public override async Task ExecuteAsync(WorkloadContext context)
    {
        Console.WriteLine($"Hello {context.WorkloadId}");
    }
}
```

It can be used as a single execution tool like Ab, but the distribution mechanism is very simple. When you start the Worker application, it will go to the connect address of the Controller by [MagicOnion](https://github.com/Cysharp/MagicOnion/)(grpc-dotnet). That's it, the connection is complete. Now all you have to do is wait for the command from the web UI.

DFrame optimizes not only for distributed performance, but also for single performance.

DFrame.Worker also supports Unity. This means that by deploying it on a large number of Headless Unity or device farms, we can load test even network frameworks that only work with Unity.

<!-- START doctoc generated TOC please keep comment here to allow auto update -->
<!-- DON'T EDIT THIS SECTION, INSTEAD RE-RUN doctoc TO UPDATE -->
## Table of Contents

- [Getting started](#getting-started)
- [Controller and Worker](#controller-and-worker)
  - [DFrame.Controller](#dframecontroller)
  - [DFrame.Worker](#dframeworker)
- [Workload](#workload)
  - [WorkloadContext](#workloadcontext)
- [Mode](#mode)
  - [Request](#request)
  - [Repeat](#repeat)
  - [Duration](#duration)
  - [Infinite](#infinite)
- [Options](#options)
  - [DFrameControllerOptions](#dframecontrolleroptions)
  - [DFrameWorkerOptions](#dframeworkeroptions)
    - [Metadata](#metadata)
- [DFrameApp/DFrameAppBuilder](#dframeappdframeappbuilder)
- [Persistent execute results](#persistent-execute-results)
- [REST API for Automation](#rest-api-for-automation)
- [Unity](#unity)
- [License](#license)

<!-- END doctoc generated TOC please keep comment here to allow auto update -->

## Getting started

For .NET, use NuGet. For Unity, please read [Unity](#unity) section.

> Install-Package [DFrame](https://nuget.org/packages/DFrame)

`DFrameApp.Run` is most simple entry point of DFrame. It runs `DFrame.Controler` and `DFrame.Worker` in single binary.

DFrame calls a test scenario a `Workload`. Your test scenario implements `Workload` and `Task ExecuteAsync(WorkloadContext context)`.

```csharp
using DFrame;

DFrameApp.Run(7312, 7313); // WebUI:7312, WorkerListen:7313

public class SampleWorkload : Workload
{
    public override async Task ExecuteAsync(WorkloadContext context)
    {
        Console.WriteLine($"Hello {context.WorkloadId}");
    }
}
```

Open the browser `http://localhost:7312`, Workload select-box has this `SampleWorkload`.

![image](https://user-images.githubusercontent.com/46207/155892546-c00f1554-0e2c-4e11-acdd-f0d9be9c40c9.png)

`ExecuteAsync` is invoked "Total Request" times. Concurrency is sometimes referred to as Virtual User in other frameworks. In DFrame, create N workloads on single worker and invoke `ExecuteAsync` in parallel.

Other overloads, `Workload` has `SetupAsync`, `TeardownAsync` and `Complete`. For example, simple gRPC test is here.

```csharp
public class GrpcTest : Workload
{
    GrpcChannel? channel;
    Greeter.GreeterClient? client;

    public override async Task SetupAsync(WorkloadContext context)
    {
        channel = GrpcChannel.ForAddress("http://localhost:5027");
        client = new Greeter.GreeterClient(channel);
    }

    public override async Task ExecuteAsync(WorkloadContext context)
    {
        await client!.SayHelloAsync(new HelloRequest(), cancellationToken: context.CancellationToken);
    }

    public override async Task TeardownAsync(WorkloadContext context)
    {
        if (channel != null)
        {
            await channel.ShutdownAsync();
            channel.Dispose();
        }
    }
}
```

You can also accept parameters, so you can create something like passing an arbitrary URL. In the constructor, you can accept parameters or an instance injected by DI.

```csharp
using DFrame;
using Microsoft.Extensions.DependencyInjection;

// use builder can configure services, logging, configuration, etc.
var builder = DFrameApp.CreateBuilder(7312, 7313);
builder.ConfigureServices(services =>
{
    services.AddSingleton<HttpClient>();
});
await builder.RunAsync();

public class HttpGetString : Workload
{
    readonly HttpClient httpClient;
    readonly string url;

    // HttpClient is from DI, URL is passed from Web UI
    public HttpGetString(HttpClient httpClient, string url)
    {
        this.httpClient = httpClient;
        this.url = url;
    }

    public override async Task ExecuteAsync(WorkloadContext context)
    {
        await httpClient.GetStringAsync(url, context.CancellationToken);
    }
}
```

![image](https://user-images.githubusercontent.com/46207/155893829-fc9f5e9d-fb05-4bcc-b8ee-6067be674b51.png)

If you want to test a simple HTTP GET/POST/PUT/DELETE, you can enable `IncludeDefaultHttpWorkload`, which will add a workload that accepts url and body parameters.

```csharp
using DFrame;

var builder = DFrameApp.CreateBuilder(7312, 7313);
builder.ConfigureWorker(x =>
{
    x.IncludesDefaultHttpWorkload = true;
});
builder.Run();
```

This option is useful if you want to try out a DFrame.

## Controller and Worker

Worker connections means multiple processes. If they are running on different servers, they can be executed concurrently from distributed servers. The Controller must always be a single process, but the Worker can launch multiple processes.

There are two ways to separate the Controller from the Worker. The first is to simply separate the projects.

![image](https://user-images.githubusercontent.com/46207/154921606-b9955331-1d15-4c4f-a769-faeb61b13872.png)

The other ways is to switch modes with command line arguments. I recommend this one as it makes local development easier. `DFrameApp.CreateBuilder` has `RunAsync`(run both), `RunControllerAsync`(run only controller), `RunWorkerAsync`(run only worker).

```csharp
using DFrame;

var builder = DFrameApp.CreateBuilder(5555, 5556); // portWeb, portListenWorker

if (args.Length == 0)
{
    // local, run both(host WebUI on http://localhost:portWeb)
    await builder.RunAsync();
}
else if (args[0] == "controller")
{
    // listen http://*:portWeb as WebUI and http://*:portListenWorker as Worker listen gRPC
    await builder.RunControllerAsync();
}
else if (args[0] == "worker")
{
    // worker connect to (controller) address.
    // You can also configure from appsettings.json via builder.ConfigureWorker((ctx, options) => { options.ControllerAddress = "" });
    await builder.RunWorkerAsync("http://foobar:5556");
}
```

### DFrame.Controller

For minimizes dependency, you can only reference `DFrame.Controller` instead of `DFrame`.

> Install-Package [DFrame.Controller](https://nuget.org/packages/DFrame.Controller)  

If you want to use `DFrame.Controller` instead of `DFrameApp`, build it from `WebApplicationBuilder` and `RunDFrameControllerAsync()`.

```csharp
using DFrame;
using Microsoft.AspNetCore.Builder;

var builder = WebApplication.CreateBuilder(args);
await builder.RunDFrameControllerAsync();
```

`DFrame.Controller` open two addresses, `Http/1` is for Web UI(built on Blazor Server), `Http/2` is for worker clusters(built on [MagicOnion](https://github.com/Cysharp/MagicOnion/)(gRPC)). You have to add `appsettings.json`(and CopyToOutputDirectory) to configure address.

```json
{
  "Kestrel": {
    "Endpoints": {
      "Http": {
        "Url": "http://localhost:7312",
        "Protocols": "Http1"
      },
      "Grpc": {
        "Url": "http://localhost:7313",
        "Protocols": "Http2"
      }
    }
  }
}
```

`DFrameApp/DFrameAppBuilder.Run()` has `string? controllerAddress = null` parameter. If does not pass any value, DFrame.Worker connect to `http://localhost:portListenWorker`. If you want to connect other server, must pass controllerAddress.

```csharp
// controller listen worker on http://*:7313 and worker connect to "http://999.99.99.99:7313".
DFrameApp.Run(7312, 7313, "http://999.99.99.99:7313");
```

### DFrame.Worker

For minimizes dependency, you can only reference `DFrame.Worker` instead of `DFrame`.

> Install-Package [DFrame.Worker](https://nuget.org/packages/DFrame.Worker)

If you want to use `DFrame.Worker` instead of `DFrameApp`, build it from `Host` and `RunDFrameWorkerAsync()`.

```csharp
using DFrame;
using Microsoft.Extensions.Hosting;

await Host.CreateDefaultBuilder(args)
    .RunDFrameWorkerAsync("http://localhost:7313"); // http/2 address to connect controller
```

## Workload

Workload is a test scenario unit you can write own. It always requires the implementation of `ExecuteAsync`, but it has three other methods.

```csharp
public abstract class Workload
{
    public abstract Task ExecuteAsync(WorkloadContext context);
    public virtual Task SetupAsync(WorkloadContext context);
    public virtual Task TeardownAsync(WorkloadContext context);
    public virtual Dictionary<string, string>? Complete(WorkloadContext context);
}
```

Here is a pseudo code of workload lifetime.

```csharp
var workloads = new Workloads[concurrency];
for (var i = 0; i < workloads.Length; i++)
{
    // actually uses ActivatorUtilities.CreateInstance to support DI and Parameters
    workloads[i] = new Workload();
}

try
{
    await Task.WhenAll(workloads.Select(workload => workload.SetupAsync());
    
    await Task.WhenAll(workloads.Select(async workload =>
    {
        for (var i = 0; i < executeCount; i++)
        {
            await workload.ExecuteAsync();
        }
    });

    foreach(var workload in workloads) workload.Complete(); // send result to server
}
finally
{
    await Task.WhenAll(workloads.Select(workload => workload.TeardownAsync());
}
```

Workload constructor can accepts DI instance or parameter. Allowed Parameter types are  all primitives(int, string, double, etc...) and `Guid` and `DateTime` and `Enum`.

```csharp
public class LogSum : Workload
{
    readonly ILogger<LoggerSum> logger;
    readonly int x;
    readonly int y;

    public LogSum(ILogger<LoggerSum> logger, int x, int y)
    {
        this.logger = logger;
        this.x = x;
        this.y = y;
    }

    public override async Task ExecuteAsync(WorkloadContext context)
    {
        logger.LogInformation($"Log Sum Parameters:{x + y}")
    }
}
```

In default, Workload name is selected from `Type.Name`. If you wan to custom name instead of type name, you can use `WorkloadAttribute`.

```csharp
[Workload("my-workload")]
public class MyWorkload : Workload
{
    // snip...
}
```

`Dictionary<string, string>? Complete()` methods return result to server. It is called after Execute complete.

```csharp
public class ReturnResult : Workload
{
    DateTime beginTime;
    DateTime endTime;
    int executeCount;

    public override async Task SetupAsync(WorkloadContext context)
    {
        beginTime = DateTime.Now;
    }

    public override async Task ExecuteAsync(WorkloadContext context)
    {
        endTime = DateTime.Now;
        executeCount++;
    }

    public override Dictionary<string, string>? Complete(WorkloadContext context)
    {
        return new()
        {
            { "begin", beginTime.ToString() },
            { "end", endTime.ToString() },
            { "count", executeCount.ToString() },
        };
    }
}
```

It can check on `...` drawer on each worker result.

![image](https://user-images.githubusercontent.com/46207/155897037-892f91d7-04da-4459-bc02-3b41cb2078ef.png)

![image](https://user-images.githubusercontent.com/46207/155897033-6d0940ff-d14a-47c3-83b7-a0b5a0907796.png)


### WorkloadContext

```csharp
public class WorkloadContext
{
    public WorkloadId WorkloadId { get; }
    public int WorkloadCount { get; }
    public int WorkloadIndex { get; }
    public CancellationToken CancellationToken { get; }
}
```

Especially `CancellationToken` is important, execution will be canceled if press Cancel or Stop from Controller. So use Async method in SetupAsync or ExecuteAsync, you should pass context.CancellationToken.

## Mode

For the execute, DFrame has four modes. Select workload, Concurrency, Worker Limit is common settings. Concurrency is create and parallel count per worker. For example 4 worker connections and 10 concurrency, DFrame create 40 instance of workload and parallel number of execute is 40. Worker Limit limits workers for execution. If No Limit, uses all worker connections.

### Request

![image](https://user-images.githubusercontent.com/46207/155897485-b99b981a-f1d7-4e71-abb2-917eb46a904c.png)

Total Request is total count of execution. Execution count per workload is total-request / worker-limit / concurrency.

### Repeat

![image](https://user-images.githubusercontent.com/46207/155897488-cb96c97c-5830-4fac-afe4-4a901491bd0b.png)

Repeat is similar as Ramp-Up. After request completed, increase TotalRequest and WorkerLimit.

### Duration

![image](https://user-images.githubusercontent.com/46207/155897499-2e7f720f-97c9-4a1a-9915-50f5a87ac547.png)

Duration has no TotalRequest. Instead, has duration seconds.

### Infinite

![image](https://user-images.githubusercontent.com/46207/155897507-9eaad698-2313-483b-a43e-5410039b27f4.png)

Infinite executes inifinitely until STOP.

## Options

Options can be configure via `DFrameAppBuilder`'s `ConfigureWorker`, `ConfigureController`.

```csharp
var builder = DFrameApp.CreateBuilder(7312, 7313);

// setup Controller options.
builder.ConfigureController((ctx, options) =>
{
    options.CompleteElapsedBufferCount = 100000;
    options.Title = "My DFrame Controller";
});

// setup Worker options.
builder.ConfigureWorker((ctx, options) =>
{
    options.Metadata = new()
    {
        { "MachineName", Environment.MachineName }
    };
    options.VirtualProcess = 4;
    options.MinBatchRate = 5000;
    options.MaxBatchRate = 10000;
});
```

If you're using `HostBuilder.RunDFrameWorkerAsync` or `WebApplicationBuilder.RunDFrameControllerAsync`, Run...Async has configure parameter.

```csharp
await Host.CreateDefaultBuilder(args).RunDFrameWorkerAsync((ctx, opt) =>
{
    opt.ControllerAddress = "http://localhost:7313";
}); 

await WebApplication.CreateBuilder(args).RunDFrameControllerAsync((ctx, opt) =>
{
    opt.Title = "foo";
});    
```

There `ctx` is HostContext, it can get configuration so you can set option from configuration.

### DFrameControllerOptions

```csharp
public class DFrameControllerOptions
{
    /// <summary>Affects to calculate median, percentile90, percentile95.</summary>
    public int CompleteElapsedBufferCount { get; set; } = 100000;

    public int ServerLogBufferCount { get; set; } = 1000;

    public string Title { get; set; } = "DFrame Controller";

    public bool DisableRestApi { get; set; } = false;
}
```

For compute median and percentile, server stored all elapsed values. However can not store all to save server memory so DFrame uses circular buffer to store it. If value over `CompleteElapsedBufferCount`, first value is out and new value is in at last. This buffer is per worker.

### DFrameWorkerOptions

```csharp
public class DFrameWorkerOptions
{
    public string ControllerAddress { get; set; } = default!;
    public TimeSpan ConnectTimeout { get; set; } = TimeSpan.FromMinutes(1);
    public TimeSpan ReconnectTime { get; set; } = TimeSpan.FromSeconds(5);
#if !UNITY_2020_1_OR_NEWER
    public SocketsHttpHandlerOptions SocketsHttpHandlerOptions { get; set; } = new SocketsHttpHandlerOptions();
#else
    public Grpc.Core.ChannelCredentials GrpcChannelCredentials { get; set; } = Grpc.Core.ChannelCredentials.Insecure;
    public IEnumerable<Grpc.Core.ChannelOption> GrpcChannelOptions { get; set; } = Array.Empty<Grpc.Core.ChannelOption>();
#endif
    public Assembly[] WorkloadAssemblies { get; set; } = AppDomain.CurrentDomain.GetAssemblies();
    public Dictionary<string, string> Metadata { get; set; } = new Dictionary<string, string>();
    public bool IncludesDefaultHttpWorkload { get; set; } = false;
    public int VirtualProcess { get; set; } = 1;
    public int MinBatchRate { get; set; } = 500;
    public int MaxBatchRate { get; set; } = 1000;
    public int BatchRate
    {
        set
        {
            MinBatchRate = MaxBatchRate = value;
        }
    }

    public DFrameWorkerOptions()
    {
    }

    public DFrameWorkerOptions(string controllerAddress)
    {
        this.ControllerAddress = controllerAddress;
    }
}

#if !UNITY_2020_1_OR_NEWER
    public class SocketsHttpHandlerOptions
    {
        public TimeSpan KeepAlivePingDelay { get; set; } = TimeSpan.FromSeconds(60);
        public TimeSpan KeepAlivePingTimeout { get; set; } = TimeSpan.FromSeconds(30);
    }
#endif
```

`ControllerAddress` has some shorthand. `DFrameApp.Run/RunAync(string? controllerAddress = null)`, `DFrameAppBuilder.Run/RunAsync/RunWorkerAsync(string? controllerAddress = null)` will set ControllerAddress if value is not null.

If you want to set ControllerAddress from configuration, use `Action<HostBuilderContext, DFrameWorkerOptions> configureWorker` parameter and get config from HostBuilderContext.

`VirtualProcess` changes the number of Worker connections in a single Process. If you change to 32, Worker connections will shown 32. This means changing the Socket for the gRPC connection to the Controller to multiple sockets. Performance may be improved if the Workload is running fast and waiting for Progress to be sent. However, if you are actually running multiple Workers, we recommend that you change this only if the Worker is a single process, as it will be confusing to distinguish between real and virtual Workers.

`BatchRate` is sent rate of progress to server. If set to 1, sent everytime. If Execute is slow, you can lower the rate because the load on the Controller is not high (but the RPS will go down because of the overhead of sending it). If Execute is fast, or if a large number of Workloads are being executed, the load on the Controller may increase. In such cases, you may want to increase the BatchRate.

The actual batch interval is changed for each transmission from MinBatchRate - MaxBatchRate. This jitter helps to reduce the load on the Controller, so it is recommended to set Min and Max instead of setting the same number.

#### Metadata

`Metadata` is sent to Contoller when connecting. It can see in the result's `...` drawer.

```csharp
var builder = DFrameApp.CreateBuilder(7312, 7313);
builder.ConfigureWorker(options =>
{
    options.Metadata = new()
    {
        { "MachineName", Environment.MachineName },
        { "ProcessorCount", Environment.ProcessorCount.ToString() }
    };
});
builder.Run();
```

![image](https://user-images.githubusercontent.com/46207/155900077-9a08cdee-6d58-4ae2-87f1-2cc837cc5501.png)

## DFrameApp/DFrameAppBuilder

`DFrameApp` setups both Controller and Worker. It has `Run()`, `RunAsync()` and  `CreateBuilder()` methods. 

`DFrameAppBuilder` has `Configure***` methods like `ConfigureServices`, `ConfigureLogging`, etc. It configure both Controller and Worker. If you want to set it to only one side, use `ControllerBuilder` or `WorkerBuilder` property.

DFrameAppBuilder also has `ConfigureController` and `ConfigureWorker`, it can configure DFrame options.

## Persistent execute results

In default, execution result is stored to in-memory so deleted when server restarted. If you want to persistent results, implements `IExecutionResultHistoryProvider` and inject it.

```csharp
public interface IExecutionResultHistoryProvider
{
    public event Action? NotifyCountChanged;
    int GetCount();
    IReadOnlyList<ExecutionSummary> GetList(); // list is shown in history page(reverse order)
    (ExecutionSummary Summary, SummarizedExecutionResult[] Results)? GetResult(ExecutionId executionId);
    void AddNewResult(ExecutionSummary summary, SummarizedExecutionResult[] results);
}
```

For exapmle, store to Database, here is sample DDL.

```csharp
CREATE TABLE dframe_results (
    execution_id string, // primary key
    start_time datetime, // create index
    summary json, // serialize ExecutionSummary
    results json,  // serialize SummarizedExecutionResult[]
);
```

`ExecutionSummary` and `SummarizedExecutionResult` is serializable(can serialize/deserialize). This is the sample of json export to file.

```csharp
public class FlatFileLogExecutionResultHistoryProvider : IExecutionResultHistoryProvider
{
    readonly string rootDir;
    readonly IExecutionResultHistoryProvider memoryProvider;

    public event Action? NotifyCountChanged;

    public FlatFileLogExecutionResultHistoryProvider(string rootDir)
    {
        this.rootDir = rootDir;
        this.memoryProvider = new InMemoryExecutionResultHistoryProvider();
    }

    public int GetCount()
    {
        return memoryProvider.GetCount();
    }

    public IReadOnlyList<ExecutionSummary> GetList()
    {
        return memoryProvider.GetList();
    }

    public (ExecutionSummary Summary, SummarizedExecutionResult[] Results)? GetResult(DFrame.Controller.ExecutionId executionId)
    {
        return memoryProvider.GetResult(executionId);
    }

    public void AddNewResult(ExecutionSummary summary, SummarizedExecutionResult[] results)
    {
        var fileName = $"{summary.StartTime.ToString("yyyy-MM-dd hh.mm.ss")} {summary.Workload} {summary.ExecutionId}";
        var json = JsonSerializer.Serialize(new { summary, results }, new JsonSerializerOptions { WriteIndented = true });

        var d = Directory.CreateDirectory(rootDir);
        Console.WriteLine(d.FullName);
        File.WriteAllText(Path.Combine(rootDir, fileName), json);

        memoryProvider.AddNewResult(summary, results);
        NotifyCountChanged?.Invoke();
    }
}
```

Created provider set to ServiceCollections as Singleton.

```csharp
var builder = DFrameApp.CreateBuilder(1000, 1001);
builder.ConfigureServices(services =>
{
    services.AddSingleton<IExecutionResultHistoryProvider>(new FlatFileLogExecutionResultHistoryProvider("results"));
});
```

## REST API for Automation

For automation, DFrame.Controller has REST API. For example `/api/connections` can get current connections count. This REST API is request/response JSON so you can handle any languages, however C# has SDK so you can use typed client.

> Install-Package [DFrame](https://nuget.org/packages/DFrame.RestSdk)

You can write like this.

```csharp
using DFrame.RestSdk;

var client = new DFrameClient("http://localhost:7312/");

// start request
await client.ExecuteRequestAsync(new()
{
    Workload = "SampleWorkload",
    Concurrency = 10,
    TotalRequest = 100000
});

// loadtest is running, wait complete.
await client.WaitUntilCanExecute();

// get summary and results[]
var result = await client.GetLatestResultAsync();
```

Which api can be used, sorry to see RestSDK's C# code. https://github.com/Cysharp/DFrame/blob/master/src/DFrame.RestSdk/DFrameClient.cs

## Unity

You can install via UPM git URL package or asset package(DFrame.*.unitypackage) available in DFrame/releases page.

* DFrame.Unity https://github.com/Cysharp/DFrame.git?path=src/DFrame.Unity/Assets/Plugins/DFrame

Andalso, you need to install dependent libraries([MagicOnion](https://github.com/Cysharp/MagicOnion/), [MessagePack](https://github.com/neuecc/MessagePack-CSharp/), gRPC).

setup details, see [MagicOnion#support-unity](https://github.com/Cysharp/MagicOnion/#support-for-unity-client) section. Code generation is no needed.

Here is the sample of connection holder and workload.

```csharp
public class DFrameWorker : MonoBehaviour
{
    DFrameWorkerApp app;

    [RuntimeInitializeOnLoadMethod]
    static void Init()
    {
        new GameObject("DFrame Worker", typeof(SampleOne));
    }

    private void Awake()
    {
        DontDestroyOnLoad(gameObject);
    }

    async void Start()
    {
        // setup your controller address
        app = new DFrameWorkerApp("localhost:7313");
        await app.RunAsync();
    }

    private void OnDestroy()
    {
        app.Dispose();
    }
}

[Preserve]
public class SampleWorkload : Workload
{
    public override Task ExecuteAsync(WorkloadContext context)
    {
        Debug.Log("Exec");
        return Task.CompletedTask;
    }

    public override Task TeardownAsync(WorkloadContext context)
    {
        Debug.Log("Teardown");
        return Task.CompletedTask;
    }
}

// Preserve for Unity IL2CPP

internal class PreserveAttribute : System.Attribute
{
}
```

![image](https://user-images.githubusercontent.com/46207/155901725-4ce8a36f-46e9-4437-aba7-639425f4b93f.png)

License
---
This library is under the MIT License.
