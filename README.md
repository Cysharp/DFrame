# DFrame
[![GitHub Actions](https://github.com/Cysharp/DFrame/workflows/Build-Debug/badge.svg)](https://github.com/Cysharp/DFrame/actions) [![Releases](https://img.shields.io/github/release/Cysharp/DFrame.svg)](https://github.com/Cysharp/DFrame/releases)

**D**istributed load-testing **Frame**work for .NET and Unity.

This library allows you to write distributed load test scenarios in C#. In addition to HTTP/1, you can test HTTP/2, gRPC, MagicOnion, Photon, or original network transport by writing in C#.

**Work In Progress** Preview `0.99.1`.

![image](https://user-images.githubusercontent.com/46207/154911899-ad34d09d-e97f-42c2-a6e2-add63ead356c.png)

 ```<div><video controls src="https://user-images.githubusercontent.com/46207/154933672-7ef38a4e-b0df-4960-9911-bdfb82b6f4f5.mp4" muted="false"></video></div>```

<!-- START doctoc generated TOC please keep comment here to allow auto update -->
<!-- DON'T EDIT THIS SECTION, INSTEAD RE-RUN doctoc TO UPDATE -->
## Table of Contents

- [Getting started](#getting-started)
- [License](#license)

<!-- END doctoc generated TOC please keep comment here to allow auto update -->

Getting started
---
Install via NuGet

> Install-Package DFrame.Controller  
> Install-Package DFrame.Worker  

DFrame has two components, Controller and Worker so you need to create two .NET applications.

![image](https://user-images.githubusercontent.com/46207/154921606-b9955331-1d15-4c4f-a769-faeb61b13872.png)

DFrame.Controller is a single ASP.NET app however you can create from ConsoleApp template.

```csharp
using DFrame;
using Microsoft.AspNetCore.Builder;

var builder = WebApplication.CreateBuilder(args);
await builder.RunDFrameControllerAsync();
```

`DFrame.Controller` open two addresses, `Http/1` is for Web UI(built on Blazor Server), `Http/2` is for worker clusters(built on [MagicOnion](https://github.com/Cysharp/MagicOnion/)(gRPC)). You have to add `appsettings.json` to configure address.

```json
{
  "Kestrel": {
    "EndpointDefaults": {
      "Protocols": "Http1AndHttp2"
    },
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

Open `http://localhost:7312"` on your browser, you can see Web UI.

`DFrame.Worker` is a worker app of clusters to write load-testing scenarios. Implement `Workload` class in your assembly.

```csharp
using DFrame;
using Microsoft.Extensions.Hosting;

await Host.CreateDefaultBuilder(args)
    .RunDFrameAsync("http://localhost:7313"); // http/2 address to connect controller
```

Test scenario(called `Workload`) can write in C#. For example, simple HTTP/1 request like here.

```csharp
public class SampleHttpWorker : Workload
{
    public override async Task ExecuteAsync(WorkloadContext context)
    {
        await new HttpClient().GetAsync("http://localhost:5000", context.CancellationToken);
    }
}
```

Workloads will listup in Controller WebUI when worker connected.

Workload alos has `SetupAsync` and `TeardownAsync` method to prepare value for execute. For example store HttpClient to field is better.

```csharp
public class SampleHttpWorker2 : Workload
{
    HttpClient httpClient = default!;

    public override async Task SetupAsync(WorkloadContext context)
    {
        httpClient = new HttpClient();
    }

    public override async Task ExecuteAsync(WorkloadContext context)
    {
        await httpClient.GetAsync("http://localhost:5000", context.CancellationToken);
    }

    public override async Task TeardownAsync(WorkloadContext context)
    {
        httpClient.Dispose();
    }
}
```

Worker constructor supports DI and parameter.

```csharp
public class SampleAppForDI : Workload
{
    readonly ILogger<SampleAppForDI> logger;

    public SampleAppForDI(ILogger<SampleAppForDI> logger)
    {
        this.logger = logger;
    }

    public override async Task ExecuteAsync(WorkloadContext context)
    {
        logger.LogInformation("Execute");
    }
}
```

If constructor parameter type is primitive(all primitives and Guid, DateTime, Enum), input form will show in Web UI and receive value from controller when execute.

```csharp
public class SampleAppForDIAndParameter : Workload
{
    readonly ILogger<SampleAppForDIAndParameter> logger;
    readonly string message;

    public SampleAppForDIAndParameter(ILogger<SampleAppForDIAndParameter> logger, string message)
    {
        this.logger = logger;
        this.message = message;
    }

    public override async Task ExecuteAsync(WorkloadContext context)
    {
        logger.LogInformation("Execute:" + message);
    }
}
```

`DFrame.Worker` is a simple daemon application. Connects to the Controller at startup and waits for execution commands. You can cluster your load tests by deploying multiple Workers.

License
---
This library is under the MIT License.
