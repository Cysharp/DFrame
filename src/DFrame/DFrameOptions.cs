﻿using MessagePack;
using MessagePack.Formatters;
using MessagePack.Resolvers;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;

namespace DFrame
{
    public class DFrameOptions
    {
        public string MasterListenHost { get; }
        public int MasterListenPort { get; }
        // TODO:Address
        public string WorkerConnectToHost { get; }
        public int WorkerConnectToPort { get; }
        public IScalingProvider ScalingProvider { get; }

        public TimeSpan Timeout { get; set; }
        public WorkerDisconnectedBehaviour WorkerDisconnectedBehaviour { get; set; }
        public MessagePackSerializerOptions SerializerOptions { get; set; }
        public Func<string?[], IHostBuilder> HostBuilderFactory { get; set; }

        public Action<ILoggingBuilder>? ConfigureInnerHostLogging { get; set; }

        public Action<ExecuteResult[], DFrameOptions, ExecuteScenario>? OnExecuteResult { get; set; } // TODO: If failed, automatically show logs?

        public DFrameOptions(string masterListenHost, int masterListenPort)
            : this(masterListenHost, masterListenPort, masterListenHost, masterListenPort, new InProcessScalingProvider())
        {
        }

        public DFrameOptions(string masterListenHost, int masterListenPort, string workerConnectToHost, int workerConnectToPort, IScalingProvider scalingProvider)
        {
            if (masterListenHost == "localhost") masterListenHost = "127.0.0.1";
            if (workerConnectToHost == "localhost") workerConnectToHost = "127.0.0.1";
            MasterListenHost = masterListenHost;
            MasterListenPort = masterListenPort;
            WorkerConnectToHost = workerConnectToHost;
            WorkerConnectToPort = workerConnectToPort;
            ScalingProvider = scalingProvider;
            Timeout = TimeSpan.FromMinutes(10);
            WorkerDisconnectedBehaviour = WorkerDisconnectedBehaviour.Stop;
            SerializerOptions = TypelessContractlessStandardResolver.Options;
            HostBuilderFactory = args => Microsoft.Extensions.Hosting.Host.CreateDefaultBuilder(args);
        }
    }

    public enum WorkerDisconnectedBehaviour
    {
        Stop,
        Continue
    }

    public struct ExecuteScenario
    {
        public string ScenarioName { get; }
        public int ProcessCount { get; }
        public int WorkerPerProcess { get; }
        public int ExecutePerWorker { get; }

        public ExecuteScenario(string scenarioName, int processCount, int workerPerProcess, int executePerWorker)
        {
            ScenarioName = scenarioName;
            ProcessCount = processCount;
            WorkerPerProcess = workerPerProcess;
            ExecutePerWorker = executePerWorker;
        }
    }
}