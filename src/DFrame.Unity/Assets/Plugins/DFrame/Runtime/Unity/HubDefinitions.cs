#nullable enable
// This file is share with DFrame.Controller and DFrame.
// Original exists in DFrame.Controller.

using MagicOnion;
using MessagePack;
using MessagePack.Formatters;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
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
        public BatchList BatchedElapsed { get; }

        public BatchedExecuteResult(WorkloadId workloadId, BatchList batchedElapsed)
        {
            WorkloadId = workloadId;
            BatchedElapsed = batchedElapsed;
        }
    }

    [MessagePackFormatter(typeof(Formatter))]
    public sealed class BatchList
    {
        long[]? forWrite;
        ReadOnlyMemory<byte>? forRead;
        int count;

        public BatchList(int capacity)
        {
            this.forWrite = new long[capacity];
            this.count = 0;
        }

        BatchList(ReadOnlyMemory<byte> data, int count)
        {
            this.forRead = data;
            this.count = count;
        }

        public ReadOnlySpan<long> AsSpan()
        {
            if (forRead != null)
            {
                return MemoryMarshal.Cast<byte, long>(forRead.Value.Span);
            }
            if (forWrite != null)
            {
                return forWrite.AsSpan(0, count);
            }
            return Array.Empty<long>();
        }

        public int Count => count;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Add(long value)
        {
            forWrite![count++] = value;
        }

        public void Clear()
        {
            this.count = 0;
        }

        class Formatter : IMessagePackFormatter<BatchList>
        {
            public void Serialize(ref MessagePackWriter writer, BatchList value, MessagePackSerializerOptions options)
            {
                var size = value.count * sizeof(long);
                writer.WriteBinHeader(size);

                var span = writer.GetSpan(size);
                MemoryMarshal.Cast<long, byte>(value.AsSpan()).CopyTo(span);
                writer.Advance(size);
            }

            public BatchList Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
            {
                var bytes = reader.ReadBytes();
                if (bytes.HasValue)
                {
                    ReadOnlyMemory<byte> memory;
                    if (bytes.Value.IsSingleSegment)
                    {
                        memory = bytes.Value.First;
                    }
                    else
                    {
                        memory = bytes.Value.ToArray().AsMemory();
                    }
                    var size = memory.Length / sizeof(long);
                    return new BatchList(memory, size);
                }

                return new BatchList(0);
            }
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