namespace DFrame.Hosting.Data
{
    public class ExecuteArgument
    {
        public int ProcessCount { get; set; }
        public int WorkerPerProcess { get; set; }
        public int ExecutePerWorker { get; set; }
        public string WorkerName { get; set; }
        public string[] Arguments { get; set; }
    }
}
