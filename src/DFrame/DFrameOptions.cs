using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;

namespace DFrame
{
    public class DFrameOptions
    {
        public string MasterListenHostAndPort { get; }
        public string WorkerConnectToHostAndPort { get; }
        public IScalingProvider ScalingProvider { get; }
        public Func<string?[], IHostBuilder> HostBuilderFactory { get; set; }

        public Action<ExecuteResult[], DFrameOptions, ExecuteScenario>? OnExecuteResult { get; set; }

        public DFrameOptions(string masterListenHostAndPort, string workerConnectToHostAndPort, IScalingProvider scalingProvider)
        {
            MasterListenHostAndPort = masterListenHostAndPort;
            WorkerConnectToHostAndPort = workerConnectToHostAndPort;
            ScalingProvider = scalingProvider;
            HostBuilderFactory = args => Microsoft.Extensions.Hosting.Host.CreateDefaultBuilder(args);
        }
    }

    public struct ExecuteScenario
    {
        public string ScenarioName { get; }
        public int NodeCount { get; }
        public int WorkerPerNode { get; }
        public int ExecutePerWorker { get; }

        public ExecuteScenario(string scenarioName, int nodeCount, int workerPerNode, int executePerWorker)
        {
            ScenarioName = scenarioName;
            NodeCount = nodeCount;
            WorkerPerNode = workerPerNode;
            ExecutePerWorker = executePerWorker;
        }
    }
}