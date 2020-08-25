namespace DFrame.Hosting.Data
{
    public class ExecuteArgument
    {
        public int ProcessCount { get; set; } = 1;
        public int WorkerPerProcess { get; set; } = 1;
        public int ExecutePerWorker { get; set; } = 1;
        public string WorkerName { get; set; }
        public string[] Arguments { get; set; }
    }
}
