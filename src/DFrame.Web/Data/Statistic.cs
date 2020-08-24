using System;

namespace DFrame.Web.Data
{
    public struct Statistic
    {
        public string Method { get; set; }
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

        public override bool Equals(object obj)
        {
            return obj is Statistic statistic &&
                   Method == statistic.Method &&
                   Name == statistic.Name &&
                   Requests == statistic.Requests &&
                   Fails == statistic.Fails &&
                   Median == statistic.Median &&
                   Percentile90 == statistic.Percentile90 &&
                   Average == statistic.Average &&
                   Min == statistic.Min &&
                   Max == statistic.Max &&
                   AverageSize == statistic.AverageSize &&
                   CurrentRps == statistic.CurrentRps &&
                   CurrentFailuresPerSec == statistic.CurrentFailuresPerSec;
        }

        public override int GetHashCode()
        {
            HashCode hash = new HashCode();
            hash.Add(Method);
            hash.Add(Name);
            hash.Add(Requests);
            hash.Add(Fails);
            hash.Add(Median);
            hash.Add(Percentile90);
            hash.Add(Average);
            hash.Add(Min);
            hash.Add(Max);
            hash.Add(AverageSize);
            hash.Add(CurrentRps);
            hash.Add(CurrentFailuresPerSec);
            return hash.ToHashCode();
        }

        public static bool operator ==(Statistic left, Statistic right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(Statistic left, Statistic right)
        {
            return !(left == right);
        }
    }
}
