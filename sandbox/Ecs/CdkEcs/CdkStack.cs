using Amazon.CDK;
using Amazon.CDK.AWS.EC2;
using Amazon.CDK.AWS.ECS;
using Amazon.CDK.AWS.IAM;
using Amazon.CDK.AWS.Logs;
using Amazon.CDK.AWS.SecretsManager;
using Amazon.CDK.AWS.ServiceDiscovery;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Cdk
{
    public class CdkStack : Stack
    {
        internal CdkStack(Construct scope, string id, IStackProps props = null) : base(scope, id, props)
        {
            // cdk deploy -c "dframeArg=request -processCount 10 -workerPerProcess 1 -executePerWorker 1 -workerName SampleHttpWorker"
            string[] dframeArgs = null;
            if (this.Node.TryGetContext("dframeArg") is string dframArg) 
                dframeArgs = dframArg.Split(" ");

            var stackProps = ReportStackProps.GetOrDefault(props);
            var echoLogGroup = "EchoServerLogGroup";
            var magiconionLogGroup = "MagicOnionServerLogGroup";
            var dframeWorkerLogGroup = "DFrameWorkerLogGroup";
            var dframeMasterLogGroup = "DFrameMasterLogGroup";

            // network
            var vpc = new Vpc(this, "Vpc", new VpcProps
            {
                MaxAzs = 2,
                NatGateways = 0,
                SubnetConfiguration = new[] { new SubnetConfiguration { Name = "public", SubnetType = SubnetType.PUBLIC } },
            });
            var allsubnets = new SubnetSelection { Subnets = vpc.PublicSubnets };
            var singleSubnets = new SubnetSelection { Subnets = new[] { vpc.PublicSubnets.First() } };
            var sg = new SecurityGroup(this, "MasterSg", new SecurityGroupProps
            {
                AllowAllOutbound = true,
                Vpc = vpc,
            });
            foreach (var subnet in vpc.PublicSubnets)
                sg.AddIngressRule(Peer.Ipv4(vpc.VpcCidrBlock), Port.AllTcp(), "VPC", true);

            // service discovery
            var serviceDiscoveryDomain = "local";
            var dframeMapName = "dframe-master";
            var echoMapName = "echo";
            var magiconionMapName = "magiconion";
            var ns = new PrivateDnsNamespace(this, "Namespace", new PrivateDnsNamespaceProps
            {
                Vpc = vpc,
                Name = serviceDiscoveryDomain,
            });

            // iam
            var iamEcsTaskExecuteRole = GetIamEcsTaskExecuteRole(new[] { dframeWorkerLogGroup, dframeMasterLogGroup });
            var iamDFrameTaskDefRole = GetIamEcsDframeMasterTaskDefRole();
            var iamWorkerTaskDefRole = GetIamEcsDframeWorkerTaskDefRole();

            // secrets
            var ddToken = stackProps.UseFargateDatadogAgentProfiler
                ? Amazon.CDK.AWS.SecretsManager.Secret.FromSecretNameV2(this, "dd-token", "dframe-datadog-token")
                : null;

            #region ECS
            var cluster = new Cluster(this, "EcsCluster", new ClusterProps { ClusterName = $"{StackName}-Cluster", Vpc = vpc, });

            // echo server
            var echoTaskDef = new FargateTaskDefinition(this, "EchoTaskDef", new FargateTaskDefinitionProps
            {
                ExecutionRole = iamEcsTaskExecuteRole,
                TaskRole = iamWorkerTaskDefRole,
                Cpu = stackProps.WorkerFargate.CpuSize,
                MemoryLimitMiB = stackProps.WorkerFargate.MemorySize,
            });
            echoTaskDef.AddContainer("server", new ContainerDefinitionOptions
            {
                Image = ContainerImage.FromRegistry("cysharp/dframe-echoserver:0.0.4"),
                Logging = LogDriver.AwsLogs(new AwsLogDriverProps
                {
                    LogGroup = new LogGroup(this, "EchoLogGroup", new LogGroupProps
                    {
                        LogGroupName = echoLogGroup,
                        RemovalPolicy = RemovalPolicy.DESTROY,
                        Retention = RetentionDays.TWO_WEEKS,
                    }),
                    StreamPrefix = echoLogGroup,
                }),
                PortMappings = new []
                {
                    new PortMapping
                    {
                        ContainerPort = 80,
                        HostPort = 80,
                    }
                }
            });
            echoTaskDef.AddDatadogContainer($"dframe-datadog", ddToken, () => stackProps.UseFargateDatadogAgentProfiler);
            var echoService = new FargateService(this, "EchoServer", new FargateServiceProps
            {
                ServiceName = "EchoServer",
                DesiredCount = 1,
                Cluster = cluster,
                TaskDefinition = echoTaskDef,
                VpcSubnets = singleSubnets,
                SecurityGroups = new[] { sg },
                PlatformVersion = FargatePlatformVersion.VERSION1_4,
                MinHealthyPercent = 0,
                AssignPublicIp = true,
                CloudMapOptions = new CloudMapOptions
                {
                    CloudMapNamespace = ns,
                    Name = echoMapName,
                    DnsRecordType = DnsRecordType.A,
                    DnsTtl = Duration.Seconds(300),
                },
            });

            // magiconion server
            var magiconionTaskDef = new FargateTaskDefinition(this, "MagicOnionTaskDef", new FargateTaskDefinitionProps
            {
                ExecutionRole = iamEcsTaskExecuteRole,
                TaskRole = iamWorkerTaskDefRole,
                Cpu = stackProps.WorkerFargate.CpuSize,
                MemoryLimitMiB = stackProps.WorkerFargate.MemorySize,
            });
            magiconionTaskDef.AddContainer("server", new ContainerDefinitionOptions
            {
                Image = ContainerImage.FromRegistry("cysharp/dframe-magiconion:0.0.4"),
                Logging = LogDriver.AwsLogs(new AwsLogDriverProps
                {
                    LogGroup = new LogGroup(this, "MagicOnionLogGroup", new LogGroupProps
                    {
                        LogGroupName = magiconionLogGroup,
                        RemovalPolicy = RemovalPolicy.DESTROY,
                        Retention = RetentionDays.TWO_WEEKS,
                    }),
                    StreamPrefix = magiconionLogGroup,
                }),
                PortMappings = new[]
                {
                    new PortMapping
                    {
                        ContainerPort = 12346,
                        HostPort = 12346,
                    }
                }
            });
            magiconionTaskDef.AddDatadogContainer($"dframe-datadog", ddToken, () => stackProps.UseFargateDatadogAgentProfiler);
            var magiconionService = new FargateService(this, "MagicOnionServer", new FargateServiceProps
            {
                ServiceName = "MagicOnionServer",
                DesiredCount = 1,
                Cluster = cluster,
                TaskDefinition = magiconionTaskDef,
                VpcSubnets = singleSubnets,
                SecurityGroups = new[] { sg },
                PlatformVersion = FargatePlatformVersion.VERSION1_4,
                MinHealthyPercent = 0,
                AssignPublicIp = true,
                CloudMapOptions = new CloudMapOptions
                {
                    CloudMapNamespace = ns,
                    Name = magiconionMapName,
                    DnsRecordType = DnsRecordType.A,
                    DnsTtl = Duration.Seconds(300),
                },
            });

            // dframe-worker
            var dframeWorkerContainerName = "worker";
            var dframeWorkerTaskDef = new FargateTaskDefinition(this, "DFrameWorkerTaskDef", new FargateTaskDefinitionProps
            {
                ExecutionRole = iamEcsTaskExecuteRole,
                TaskRole = iamWorkerTaskDefRole,
                Cpu = stackProps.WorkerFargate.CpuSize,
                MemoryLimitMiB = stackProps.WorkerFargate.MemorySize,
            });
            dframeWorkerTaskDef.AddContainer(dframeWorkerContainerName, new ContainerDefinitionOptions
            {
                Image = ContainerImage.FromRegistry("cysharp/dframe-consoleappecs:0.0.4"),
                Command = new[] { "--worker-flag" },
                Environment = new Dictionary<string, string>
                {
                    { "DFRAME_MASTER_CONNECT_TO_HOST", $"{dframeMapName}.{serviceDiscoveryDomain}"},
                    { "DFRAME_MASTER_CONNECT_TO_PORT", "12345"},
                    { "BENCH_HTTP_SERVER_HOST", $"http://{echoMapName}.{serviceDiscoveryDomain}" },
                    { "BENCH_GRPC_SERVER_HOST", $"http://{magiconionMapName}.{serviceDiscoveryDomain}:12346" },
                },
                Logging = LogDriver.AwsLogs(new AwsLogDriverProps
                {
                    LogGroup = new LogGroup(this, "WorkerLogGroup", new LogGroupProps
                    {
                        LogGroupName = dframeWorkerLogGroup,
                        RemovalPolicy = RemovalPolicy.DESTROY,
                        Retention = RetentionDays.TWO_WEEKS,
                    }),
                    StreamPrefix = dframeWorkerLogGroup,
                }),
            });
            dframeWorkerTaskDef.AddDatadogContainer($"{dframeWorkerContainerName}-datadog", ddToken, () => stackProps.UseFargateDatadogAgentProfiler);
            var dframeWorkerService = new FargateService(this, "DFrameWorkerService", new FargateServiceProps
            {
                ServiceName = "DFrameWorkerService",
                DesiredCount = 0,
                Cluster = cluster,
                TaskDefinition = dframeWorkerTaskDef,
                VpcSubnets = singleSubnets,
                SecurityGroups = new[] { sg },
                PlatformVersion = FargatePlatformVersion.VERSION1_4,
                MinHealthyPercent = 0,
                AssignPublicIp = true,
            });

            // dframe-master
            var dframeMasterTaskDef = new FargateTaskDefinition(this, "DFrameMasterTaskDef", new FargateTaskDefinitionProps
            {
                ExecutionRole = iamEcsTaskExecuteRole,
                TaskRole = iamDFrameTaskDefRole,
                Cpu = stackProps.MasterFargate.CpuSize,
                MemoryLimitMiB = stackProps.MasterFargate.MemorySize,
            });
            dframeMasterTaskDef.AddContainer("dframe", new ContainerDefinitionOptions
            {
                Image = ContainerImage.FromRegistry("cysharp/dframe-consoleappecs:0.0.4"),
                Command = dframeArgs,
                Environment = new Dictionary<string, string>
                {
                    { "DFRAME_CLUSTER_NAME", cluster.ClusterName },
                    { "DFRAME_MASTER_SERVICE_NAME", "DFrameMasterService" },
                    { "DFRAME_WORKER_CONTAINER_NAME", dframeWorkerContainerName },
                    { "DFRAME_WORKER_SERVICE_NAME", dframeWorkerService.ServiceName },
                    { "DFRAME_WORKER_TASK_NAME", Fn.Select(1, Fn.Split("/", dframeWorkerTaskDef.TaskDefinitionArn)) },
                    { "DFRAME_WORKER_IMAGE", "cysharp/dframe-consoleappecs:0.0.4" },
                },
                Logging = LogDriver.AwsLogs(new AwsLogDriverProps
                {
                    LogGroup = new LogGroup(this, "MasterLogGroup", new LogGroupProps
                    {
                        LogGroupName = dframeMasterLogGroup,
                        RemovalPolicy = RemovalPolicy.DESTROY,
                        Retention = RetentionDays.TWO_WEEKS,
                    }),
                    StreamPrefix = dframeMasterLogGroup,
                }),
            });
            dframeMasterTaskDef.AddDatadogContainer($"dframe-datadog", ddToken, () => stackProps.UseFargateDatadogAgentProfiler);
            var dframeMasterService = new FargateService(this, "DFrameMasterService", new FargateServiceProps
            {
                ServiceName = "DFrameMasterService",
                DesiredCount = 1,
                Cluster = cluster,
                TaskDefinition = dframeMasterTaskDef,
                VpcSubnets = singleSubnets,
                SecurityGroups = new[] { sg },
                PlatformVersion = FargatePlatformVersion.VERSION1_4,
                MinHealthyPercent = 0,
                AssignPublicIp = true,
                CloudMapOptions = new CloudMapOptions
                {
                    CloudMapNamespace = ns,
                    Name = dframeMapName,
                    DnsRecordType = DnsRecordType.A,
                    DnsTtl = Duration.Seconds(300),
                },
            });

            // dframMasterService must create after echoService/magiconionService
            dframeMasterService.Node.AddDependency(echoService);
            dframeMasterService.Node.AddDependency(magiconionTaskDef); 

            #endregion

            // output
            new CfnOutput(this, "EcsClusterName", new CfnOutputProps { Value = cluster.ClusterName });
        }

        private Role GetIamEcsTaskExecuteRole(string[] logGroups)
        {
            var policy = new Policy(this, "DframeEcsTaskExecutionPolicy", new PolicyProps
            {
                Statements = new[]
                {
                    new PolicyStatement(new PolicyStatementProps
                    {
                        Actions = new[]
                        {
                            "logs:CreateLogStream",
                            "logs:PutLogEvents"
                        },
                        Resources = logGroups.Select(x => $"arn:aws:logs:{this.Region}:{this.Account}:log-group:{x}:*").ToArray(),
                    }),
                }
            });
            var role = new Role(this, "DframeEcsTaskExecutionRole", new RoleProps
            {
                AssumedBy = new ServicePrincipal("ecs-tasks.amazonaws.com"),
            });
            role.AttachInlinePolicy(policy);
            role.AddManagedPolicy(ManagedPolicy.FromManagedPolicyArn(this, "DframeAmazonECSTaskExecutionRolePolicy", "arn:aws:iam::aws:policy/service-role/AmazonECSTaskExecutionRolePolicy"));
            return role;
        }
        private Role GetIamEcsDframeMasterTaskDefRole()
        {
            // master need to control ecs service and taskdef
            var policy = new Policy(this, "DframeMasterTaskDefTaskPolicy", new PolicyProps
            {
                Statements = new[]
                {
                    // ecs
                    new PolicyStatement(new PolicyStatementProps
                    {
                        Actions = new[]
                        {
                            "ecs:Describe*",
                            "ecs:List*",
                            "ecs:Update*",
                            "ecs:DiscoverPollEndpoint",
                            "ecs:Poll",
                            "ecs:RegisterContainerInstance",
                            "ecs:RegisterTaskDefinition",
                            "ecs:StartTelemetrySession",
                            "ecs:UpdateContainerInstancesState",
                            "ecs:Submit*",
                        },
                        Resources = new[] { "*" },
                    }),
                    new PolicyStatement(new PolicyStatementProps
                    {
                        Actions = new []
                        {
                            "iam:PassRole",
                        },
                        Resources = new [] { "*" },
                    }),
                }
            });
            var role = new Role(this, "DframeMasterTaskDefTaskRole", new RoleProps
            {
                AssumedBy = new ServicePrincipal("ecs-tasks.amazonaws.com"),
            });
            role.AttachInlinePolicy(policy);
            return role;
        }
        private Role GetIamEcsDframeWorkerTaskDefRole()
        {
            // worker and other doesn't need any permission.
            var policy = new Policy(this, "DframeWorkerTaskDefTaskPolicy", new PolicyProps
            {
            });
            var role = new Role(this, "DframeWorkerTaskDefTaskRole", new RoleProps
            {
                AssumedBy = new ServicePrincipal("ecs-tasks.amazonaws.com"),
            });
            role.AttachInlinePolicy(policy);
            return role;
        }
    }

    public static class TaskDefinitionExtensions
    {
        public static void AddDatadogContainer(this TaskDefinition taskdef, string containerName, ISecret ddToken, Func<bool> enable)
        {
            if (enable != null && enable.Invoke())
            {
                taskdef.AddContainer(containerName, new ContainerDefinitionOptions
                {
                    Image = ContainerImage.FromRegistry("datadog/agent:latest"),
                    Environment = new Dictionary<string, string>
                    {
                        { "DD_API_KEY", ddToken.SecretValue.ToString() },
                        { "ECS_FARGATE","true"},
                    },
                    Cpu = 10,
                    MemoryReservationMiB = 256,
                    Essential = false,
                });
            }
        }
    }

}
