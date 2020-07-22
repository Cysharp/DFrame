using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;

namespace DFrame
{
    public class DFrameOptions
    {
        public string MasterListenHost { get; }
        public int MasterListenPort { get; }
        public string WorkerConnectToHost { get; }
        public int WorkerConnectToPort { get; }
        public IScalingProvider ScalingProvider { get; }
        public Func<string?[], IHostBuilder> HostBuilderFactory { get; set; }

        public Action<ExecuteResult[], DFrameOptions, ExecuteScenario>? OnExecuteResult { get; set; }

        public DFrameOptions(string masterListenHost, int masterListenPort)
            : this(masterListenHost, masterListenPort, masterListenHost, masterListenPort, new InProcessScalingProvider())
        {
        }

        public DFrameOptions(string masterListenHost, int masterListenPort, string workerConnectToHost, int workerConnectToPort, IScalingProvider scalingProvider)
        {
            MasterListenHost = masterListenHost;
            MasterListenPort = masterListenPort;
            WorkerConnectToHost = workerConnectToHost;
            WorkerConnectToPort = workerConnectToPort;
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