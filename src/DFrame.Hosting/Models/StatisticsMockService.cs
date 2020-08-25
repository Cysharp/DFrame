using DFrame.Hosting.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DFrame.Hosting.Models
{
    /// <summary>
    /// Mock data
    /// </summary>
    public class StatisticsMockService : IStatisticsService
    {
        private Dictionary<(string method, string name), int> _requests;
        private IExecuteContext _executeContext;

        public event Action<Statistic> OnUpdateStatistics;

        public void RegisterContext(IExecuteContext executeContext)
        {
            _executeContext = executeContext;
        }

        public Task<(Statistic[] statistics, Statistic aggregated)> GetStatisticsAsync()
        {
            var rnd = new Random();
            _requests = new Dictionary<(string type, string name), int>();

            var temp = new List<string>(MockData.Paths);
            var statistics = Enumerable.Range(1, 5)
                .Select(x => GenerateMockData(x, rnd, temp))
                .OrderBy(x => x.Name)
                .ToArray();
            var aggregated = AggregateStatistics(statistics);

            OnUpdateStatistics?.Invoke(aggregated);

            return Task.FromResult((statistics, aggregated));
        }

        private Statistic GenerateMockData(int index, Random rnd, IList<string> temp)
        {
            var method = MockData.HttpTypes[rnd.Next(MockData.HttpTypes.Length)];
            var path = temp[rnd.Next(temp.Count)];
            temp.Remove(path);
            var req = rnd.Next(index, 20000);
            _requests.Add((method, path), req);
            var fail = rnd.Next(0, 1000);

            var first = rnd.Next(50, 80);
            var second = 100 - rnd.Next(81, 90);
            var third = 100 - rnd.Next(91, 95);
            var last = 100 - first - second - third;
            var reqFirst = req / 100 * first;
            var reqSecond = req / 100 * second;
            var reqThird = req / 100 * third;
            var reqLast = last > 0 ? req / 100 * last : 0;
            var res = Enumerable.Range(0, reqFirst)
                .Select(x => new RequestData((double)rnd.Next(2, rnd.Next(2, 100)), rnd.Next(10, 100)))
                .Concat(Enumerable.Range(0, reqSecond)
                    .Select(x => new RequestData((double)rnd.Next(5, rnd.Next(5, 500)), rnd.Next(20, rnd.Next(20, 200))))
                )
                .Concat(Enumerable.Range(0, reqThird)
                    .Select(x => new RequestData((double)rnd.Next(20, rnd.Next(20, 500)), rnd.Next(20, rnd.Next(20, 500))))
                )
                .Concat(Enumerable.Range(0, reqLast)
                    .Select(x => new RequestData((double)rnd.Next(50, rnd.Next(50, 5000)), rnd.Next(50, rnd.Next(50, 5000))))
                );
            var sortedRes = res.OrderBy(x => x.Request).ToArray();
            var sortedResReq = sortedRes.Select(x => x.Request).ToArray();

            var timePast = TimeSpan.FromSeconds(rnd.Next(1, 30));

            return sortedResReq.Length != 0
                ? new Statistic
                {
                    Method = method,
                    Name = path,
                    Requests = req,
                    Fails = fail,
                    Median = Median(sortedResReq),
                    Percentile90 = Percentile(sortedResReq, 90),
                    Average = sortedResReq.Average(),
                    Min = sortedResReq.Min(),
                    Max = sortedResReq.Max(),
                    AverageSize = sortedRes.Select(x => x.Size).Average(),
                    CurrentRps = req / timePast.TotalSeconds,
                    CurrentFailuresPerSec = fail / timePast.TotalSeconds,
                }
                : new Statistic
                {
                    Method = method,
                    Name = path,
                    Requests = req,
                    Fails = fail,
                    Median = 0.0,
                    Percentile90 = 0.0,
                    Average = 0.0,
                    Min = 0.0,
                    Max = 0.0,
                    AverageSize = 0,
                    CurrentRps = req / timePast.TotalSeconds,
                    CurrentFailuresPerSec = fail / timePast.TotalSeconds,
                };
        }

        /// <summary>
        /// Calculate aggregated statistics
        /// </summary>
        /// <param name="statistics"></param>
        /// <returns></returns>
        private Statistic AggregateStatistics(Statistic[] statistics)
        {
            return new Statistic
            {
                Method = "",
                Name = "Aggregated",
                Requests = statistics.Sum(x => x.Requests),
                Fails = statistics.Sum(x => x.Fails),
                // todo: aggregated data calculations for Median and 90%tile. need all datas....
                Median = Median(statistics.Select(x => x.Median).ToArray()),
                Percentile90 = Percentile(statistics.Select(x => x.Percentile90).ToArray(), 90),
                Average = statistics.Average(x => x.Average),
                Min = statistics.Min(x => x.Min),
                Max = statistics.Max(x => x.Max),
                AverageSize = statistics.Average(x => x.AverageSize),
                CurrentRps = statistics.Sum(x => x.CurrentRps),
                CurrentFailuresPerSec = statistics.Sum(x => x.CurrentFailuresPerSec),
            };
        }

        /// <summary>
        /// Calculate Percentile with Interpolation.
        /// </summary>
        /// <param name="sortedSequence"></param>
        /// <param name="percentile"></param>
        /// <returns></returns>
        private static double Percentile(double[] sortedSequence, double percentile)
        {
            var n = sortedSequence.Length;
            var realIndex = Round((n + 1) * percentile) - 1;
            var rank = (int)realIndex;
            var flac = realIndex - rank;
            if (rank >= n)
            {
                // last
                return sortedSequence[n - 1];
            }
            else if (flac == 0)
            {
                // when index match to rank
                return sortedSequence[rank];
            }
            else if (rank + 1 < n)
            {
                // calculate interpolation
                return Round(sortedSequence[rank] + (sortedSequence[rank + 1] - sortedSequence[rank]) * flac);
            }
            else
            {
                return sortedSequence[rank];
            }
        }
        private static double Round(double value, int digit = 2)
        {
            return Math.Round(value, digit, MidpointRounding.AwayFromZero);
        }
        /// <summary>
        /// Calculate Median.
        /// </summary>
        /// <param name="sortedSequence"></param>
        /// <returns></returns>
        private double Median(double[] sortedSequence)
        {
            double medianValue = 0;
            if (sortedSequence.Length % 2 == 0)
            {
                // count is even, need to get the middle two elements, add them together, then divide by 2
                var middleElement1 = sortedSequence[(sortedSequence.Length / 2) - 1];
                var middleElement2 = sortedSequence[sortedSequence.Length / 2];
                medianValue = (middleElement1 + middleElement2) / 2;
            }
            else
            {
                // count is odd, simply get the middle element.
                medianValue = sortedSequence[sortedSequence.Length / 2];
            }

            return medianValue;
        }

        public struct RequestData
        {
            public readonly double Request;
            public readonly int Size;

            public RequestData(double request, int size)
            {
                Request = request;
                Size = size;
            }

            public override bool Equals(object obj)
            {
                return obj is RequestData data &&
                       Request == data.Request &&
                       Size == data.Size;
            }

            public override int GetHashCode()
            {
                return HashCode.Combine(Request, Size);
            }

            public static bool operator ==(RequestData left, RequestData right)
            {
                return left.Equals(right);
            }

            public static bool operator !=(RequestData left, RequestData right)
            {
                return !(left == right);
            }
        }
    }
}
