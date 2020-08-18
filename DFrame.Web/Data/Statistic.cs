using System;

namespace DFrame.Web.Data
{
    public struct Statistic
    {
        public string Type { get; set; }
        public string Name { get; set; }
        public int Requests { get; set; }
        public int Fails { get; set; }
        public double Median { get; set; }
        public double Percentile90 { get; set; }
        public double Average { get; set; }
        public double Min { get; set; }
        public double Max { get; set; }
        public double AverageSize { get; set; }
        public double CurrentRps { get; set; }
        public double CurrentFailuresPerSec { get; set; }
    }
}
