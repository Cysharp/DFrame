using Amazon.CDK;
using System.Collections.Generic;

namespace Cdk
{
    sealed class Program
    {
        public static void Main(string[] args)
        {
            var app = new App();
            new CdkStack(app, "DFrameCdkStack", new ReportStackProps
            {
                UseFargateDatadogAgentProfiler = false,
                MasterFargate = new Fargate(Fargate.CpuSpec.Half, Fargate.MemorySpec.Low),
                WorkerFargate = new Fargate(Fargate.CpuSpec.Half, Fargate.MemorySpec.Low),
                Tags = new Dictionary<string, string>()
                {
                    { "cf-stack", "DFrameCdkStack" },
                },
            });
            app.Synth();
        }
    }

    public class ReportStackProps : StackProps
    {
        /// <summary>
        /// Install Datadog Agent as Fargate sidecar container.
        /// </summary>
        public bool UseFargateDatadogAgentProfiler { get; set; }
        /// <summary>
        /// Fargate of Dframe master 
        /// </summary>
        public Fargate MasterFargate { get; set; }
        /// <summary>
        /// Fargate of Dframe worker
        /// </summary>
        public Fargate WorkerFargate { get; set; }

        public static ReportStackProps GetOrDefault(IStackProps props, ReportStackProps @default = null)
        {
            if (props is ReportStackProps r)
            {
                return r;
            }
            else
            {
                return @default != null ? @default : new ReportStackProps();
            }
        }
    }
}
