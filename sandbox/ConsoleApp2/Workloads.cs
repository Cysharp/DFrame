using DFrame;
using Grpc.Net.Client;
using MagicOnion;
using MagicOnion.Client;
using MessagePack;
using Microsoft.Extensions.Logging;

[Workload("myworkload")]
public class TrialWorkload : Workload
{
    readonly ILogger<TrialWorkload> logger;
    int execCount = 0;

    public TrialWorkload(ILogger<TrialWorkload> logger)
    {
        this.logger = logger;
    }

    public override Task SetupAsync(WorkloadContext context)
    {
        logger.LogInformation("Called Setup");
        return Task.CompletedTask;
    }

    public override async Task ExecuteAsync(WorkloadContext context)
    {
        logger.LogInformation("Execute:" + (++execCount));
        await Task.Delay(TimeSpan.FromSeconds(1));
    }

    public override Task TeardownAsync(WorkloadContext context)
    {
        logger.LogInformation("Called Teardown");
        return Task.CompletedTask;
    }

    public override Dictionary<string, string>? Complete(WorkloadContext context)
    {
        return new()
        {
            { "ok", "foo" },
            { "nong", "bar" },
        };
    }
}

[Workload("myworkload2")]
public class TrialWorkload2 : Workload
{
    readonly ILogger<TrialWorkload2> logger;
    readonly int x;
    readonly int y;

    public TrialWorkload2(ILogger<TrialWorkload2> logger, int x, int y)
    {
        this.logger = logger;
        this.x = x;
        this.y = y;
    }

    public override Task SetupAsync(WorkloadContext context)
    {
        return base.SetupAsync(context);
    }

    public override async Task ExecuteAsync(WorkloadContext context)
    {
        await Task.Yield();
        logger.LogInformation("Param:" + (x, y));
    }
}

[Workload("parapara")]
public class TrialWorkload3 : Workload
{
    readonly ILogger<TrialWorkload2> logger;
    readonly (bool bnothing, bool? bnullable, bool t, bool f) t;

    public TrialWorkload3(ILogger<TrialWorkload2> logger, bool bnothing, bool? bnullable, bool t = true, bool f = false)
    {
        this.logger = logger;
        this.t = (bnothing, bnullable, t, f);
    }

    public override async Task ExecuteAsync(WorkloadContext context)
    {
        await Task.Yield();
        logger.LogInformation("Param:" + t);
    }
}

public class checkarg : Workload
{

    readonly ILogger<checkarg> logger;

    public checkarg(ILogger<checkarg> logger,
        string nonDefaultString,
        int[] arrayInt,
        IroIrrrrrrrrro? nullableEnum,
        IroIrrrrrrrrro[] enumArray,
        IroIrrrrrrrrro myenum, IroIrrrrrrrrro enummSelected = IroIrrrrrrrrro.Tako,
        string defaultString = "foo",
        long myLongLongWIthDefault = 10,
        double d = 10.212,
        int? thenullableInt = null
        )
    {
        this.logger = logger;
        logger.LogInformation((nonDefaultString, string.Join(",", arrayInt), nullableEnum, string.Join(",", enumArray), myenum, enummSelected, defaultString, myLongLongWIthDefault, d, thenullableInt).ToString());
    }

    public override Task SetupAsync(WorkloadContext context)
    {
        return base.SetupAsync(context);
    }

    public override Task ExecuteAsync(WorkloadContext context)
    {
        return Task.CompletedTask;
    }
}

public enum IroIrrrrrrrrro
{
    Hoge, Huga, Tako, Foo, Bar, BazBaz
}


public class Echo5000 : Workload
{
    HttpClient httpClient = default!;

    public override Task SetupAsync(WorkloadContext context)
    {
        this.httpClient = new HttpClient();
        return Task.CompletedTask;
    }

    public override async Task ExecuteAsync(WorkloadContext context)
    {
        await httpClient.GetStringAsync("http://localhost:5111");
    }

    public override Task TeardownAsync(WorkloadContext context)
    {
        httpClient.Dispose();
        return base.TeardownAsync(context);
    }
}

public class EchoMagicOnion : Workload
{
    GrpcChannel channel = default!;
    IEchoService client = default!;

    public override async Task SetupAsync(WorkloadContext context)
    {
        channel = Grpc.Net.Client.GrpcChannel.ForAddress("http://localhost:5059");
        client = MagicOnionClient.Create<IEchoService>(channel);

        await client.Echo(""); // test echo.
    }

    public override async Task ExecuteAsync(WorkloadContext context)
    {
        await client.Echo("");
    }

    public override async Task TeardownAsync(WorkloadContext context)
    {
        await channel.ShutdownAsync();
        channel.Dispose();
    }
}

public interface IEchoService : IService<IEchoService>
{
    UnaryResult<Nil> Echo(string message);
}

[Workload("partly-cloudy")]
public class PartlyCloudy : Workload
{
    readonly ILogger<PartlyCloudy> logger;
    int execCount = 0;

    public PartlyCloudy(ILogger<PartlyCloudy> logger)
    {
        this.logger = logger;
    }

    public override Task SetupAsync(WorkloadContext context)
    {
        logger.LogInformation("Called Setup");
        return Task.CompletedTask;
    }

    public override async Task ExecuteAsync(WorkloadContext context)
    {
        if (new Random().Next(0, 2) < 1)
            throw new Exception("It's cloudy today...");

        logger.LogInformation("Execute:" + (++execCount));
        await Task.Delay(TimeSpan.FromSeconds(1));
    }

    public override Task TeardownAsync(WorkloadContext context)
    {
        logger.LogInformation("Called Teardown");
        return Task.CompletedTask;
    }
}


public class LoggerSum : Workload
{
    readonly ILogger<LoggerSum> logger;
    readonly int x;
    readonly int y;

    public LoggerSum(ILogger<LoggerSum> logger, int x, int y)
    {
        this.logger = logger;
        this.x = x;
        this.y = y;
        logger.LogInformation($"Parameter passed {(x, y)}");
    }

    public override Task ExecuteAsync(WorkloadContext context)
    {
        return Task.CompletedTask;
    }
}

public class Selection : Workload
{
    record SampleRecord(int Id, string Title, int Value);

    static readonly SampleRecord[] sampleRecords = new SampleRecord[]
    {
        new(1001, "Record1", 10),
        new(1002, "Record2", 11),
        new(1003, "Record3", 12),
        new(1004, "Record4", 13),
    };

    static IEnumerable<(string, int)> GetParameter1Selection()
        => sampleRecords.Select(x => (x.Title, x.Id));

    readonly ILogger<Selection> logger;
    readonly int parameter1;

    public Selection(ILogger<Selection> logger, [SelectionFrom(nameof(GetParameter1Selection))] int parameter1)
    {
        this.logger = logger;
        this.parameter1 = parameter1;
    }

    public override Task ExecuteAsync(WorkloadContext context)
    {
        var record = sampleRecords.FirstOrDefault(x => x.Id == parameter1)!;
        logger.LogInformation("Id:{0}, Title:{1}, Value:{2}", record.Id, record.Title, record.Value);
        return Task.CompletedTask;
    }
}

