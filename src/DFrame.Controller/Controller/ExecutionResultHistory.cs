namespace DFrame.Controller
{
    public interface IExecutionResultHistory
    {
        int Count { get; }

        ExecutionResultSummary[] GetList();
        SummarizedExecutionResult[] GetResult(ExecutionId executionId);
    }

    public class ExecutionResultSummary
    {
        // ExecutionId executionId;
    }


    public class ExecutionResultHistory
    {
        public int Count { get; set; }
    }
}
