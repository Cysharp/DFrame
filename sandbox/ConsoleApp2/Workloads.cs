using DFrame;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

[Workload("myworkload")]
public class TrialWorkload : Workload
{
    readonly ILogger<TrialWorkload> logger;

    public TrialWorkload(ILogger<TrialWorkload> logger)
    {
        this.logger = logger;
    }

    public override async Task ExecuteAsync(WorkloadContext context)
    {
        logger.LogInformation("Begin:" + context.WorkloadId);
        await Task.Yield();
        await Task.Delay(TimeSpan.FromSeconds(1));
        logger.LogInformation("End:" + context.WorkloadId);
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
        logger.LogInformation((nonDefaultString, string.Join(",", arrayInt),nullableEnum, string.Join(",", enumArray), myenum, enummSelected, defaultString, myLongLongWIthDefault, d, thenullableInt).ToString());
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