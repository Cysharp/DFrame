#nullable enable
// This file is share with DFrame.Controller and DFrame.
// Original exists in DFrame.Controller.

using MagicOnion;
using MessagePack;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DFrame
{
    public interface IControllerHub : IStreamingHub<IControllerHub, IWorkerReceiver>
    {
        Task ConnectAsync(WorkloadInfo[] workloads, Dictionary<string, string> metadata);
        Task CreateWorkloadCompleteAsync(ExecutionId executionId);
        Task ReportProgressAsync(ExecuteResult result);
        Task ReportProgressBatchedAsync(BatchedExecuteResult result);
        Task ExecuteCompleteAsync(Dictionary<WorkloadId, Dictionary<string, string>?> results);
        Task TeardownCompleteAsync();
    }

    public interface IWorkerReceiver
    {
        void CreateWorkloadAndSetup(ExecutionId executionId, int createCount, string workloadName, KeyValuePair<string, string>[] parameters);
        void Execute(long[] executeCount); // exec count per workload
        void Stop();
        void Teardown();
    }

    public readonly partial struct ExecutionId { }

    public readonly partial struct WorkerId { }

    public readonly partial struct WorkloadId { }

    [MessagePackObject]
    public class ExecuteResult
    {
        [Key(0)]
        public WorkloadId WorkloadId { get; }
        [Key(1)]
        public TimeSpan Elapsed { get; }
        [Key(2)]
        public long ExecutionNo { get; }
        [Key(3)]
        public bool HasError { get; }
        [Key(4)]
        public string? ErrorMessage { get; }

        public ExecuteResult(WorkloadId workloadId, TimeSpan elapsed, long executionNo, bool hasError, string? errorMessage)
        {
            WorkloadId = workloadId;
            Elapsed = elapsed;
            ExecutionNo = executionNo;
            HasError = hasError;
            ErrorMessage = errorMessage;
        }
    }

    [MessagePackObject]
    public class BatchedExecuteResult
    {
        [Key(0)]
        public WorkloadId WorkloadId { get; }

        [Key(1)]
        public List<long> BatchedElapsed { get; }

        public BatchedExecuteResult(WorkloadId workloadId, List<long> batchedElapsed)
        {
            WorkloadId = workloadId;
            BatchedElapsed = batchedElapsed;
        }
    }

    [MessagePackObject]
    public class WorkloadInfo
    {
        [Key(0)]
        public string Name { get; }

        // Selections => Enum Names or True/False for boolean
        [Key(1)]
        public WorkloadParameterInfo[] Arguments { get; }

        public WorkloadInfo(string name, WorkloadParameterInfo[] arguments)
        {
            Name = name;
            Arguments = arguments;
        }
    }

    [MessagePackObject]
    public class WorkloadParameterInfo
    {
        public WorkloadParameterInfo(AllowParameterType parameterType, bool isNullable, bool isArray, object? defaultValue, string parameterName, string[] enumNames, string? enumTypeName)
        {
            ParameterType = parameterType;
            IsNullable = isNullable;
            IsArray = isArray;
            DefaultValue = defaultValue;
            ParameterName = parameterName;
            EnumNames = enumNames;
            EnumTypeName = enumTypeName;
        }

        [Key(0)]
        public AllowParameterType ParameterType { get; }
        [Key(1)]
        public bool IsNullable { get; }
        [Key(2)]
        public bool IsArray { get; }
        [Key(3)]
        public object? DefaultValue { get; }
        [Key(4)]
        public string ParameterName { get; }
        [Key(5)]
        public string[] EnumNames { get; }
        [Key(6)]
        public string? EnumTypeName { get; }

        public string GetTypeLabel()
        {
            var typeName = (EnumTypeName != null) ? EnumTypeName : ParameterType.ToString();

            if (!IsArray && !IsNullable) return typeName;
            if (IsArray && !IsNullable) return $"{typeName}[]";
            if (IsArray && IsNullable) return $"{typeName}[]?";
            if (IsNullable) return $"{typeName}?";
            return typeName.ToString();
        }
    }

    public enum AllowParameterType
    {
        // Primitives + Guid + DateTime + Enum
        Enum,
        Guid,
        Boolean,
        Char,
        SByte,
        Byte,
        Int16,
        UInt16,
        Int32,
        UInt32,
        Int64,
        UInt64,
        Single,
        Double,
        Decimal,
        DateTime,
        String,
    }
}