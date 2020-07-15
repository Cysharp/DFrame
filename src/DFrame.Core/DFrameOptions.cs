using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;

namespace DFrame
{
    public class DFrameOptions
    {
        public string Host { get; }
        public int Port { get; }
        public IScalingProvider ScalingProvider { get; }
        public Func<string?[], IHostBuilder> HostBuilderFactory { get; set; }

        public Action<ExecuteResult[], DFrameOptions, ExecuteScenario>? OnExecuteResult { get; set; }

        public DFrameOptions(string host, int port, IScalingProvider scalingProvider)
        {
            Host = host;
            Port = port;
            ScalingProvider = scalingProvider;
            // TODO:configuration?
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