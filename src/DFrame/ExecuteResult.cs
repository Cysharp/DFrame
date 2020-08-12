using MessagePack;
using System;

namespace DFrame
{
    [MessagePackObject]
    public class ExecuteResult
    {
        [Key(0)]
        public string WorkerId { get; }
        [Key(1)]
        public TimeSpan Elapsed { get; }
        [Key(2)]
        public int ExecutionNo { get; }
        [Key(3)]
        public bool HasError { get; }
        [Key(4)]
        public string? ErrorMessage { get; }

        public ExecuteResult(string workerId, TimeSpan elapsed, int executionNo, bool hasError, string? errorMessage)
        {
            WorkerId = workerId;
            Elapsed = elapsed;
            ExecutionNo = executionNo;
            HasError = hasError;
            ErrorMessage = errorMessage;
        }
    }
}