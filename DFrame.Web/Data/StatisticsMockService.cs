using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace DFrame.Web.Data
{
    public interface IStatisticsService
    {
        /// <summary>
        /// Host Address to load test
        /// </summary>
        string HostAddress { get; set; }

        /// <summary>
        /// Get statistics
        /// </summary>
        /// <returns></returns>
        public Task<List<Statistic>> GetStatisticsAsync();
        /// <summary>
        /// Get failures
        /// </summary>
        /// <returns></returns>
        public Task<Failure[]> GetFailuresAsync();
    }

    /// <summary>
    /// Mock data
    /// </summary>
    public class StatisticsMockService : IStatisticsService
    {
        private static readonly string[] httpTypes = new[]
        {
            "Get", "Patch", "Post", "Put", "Delete",
        };

        private static readonly string[] httpNames = new[]
        {
            "/",
            "/Hello", "/Item", "/World",
            "/Hoge", "/Fuga", "/Piyo", "/Foo", "/Bar",
            "/Logout", "/Login", "/Auth", "/Register",
            "/Healthz", "/Liveness", "/Readiness", "/Stats",
            "/Begin", "/Questions", "/Faq", "/Post", "/Tasks", "/Cards", "/Display", "/Report",
        };

        private Dictionary<(string type, string name), int> _requests;
        private Dictionary<(string type, string name), int> _fails;

        public string HostAddress { get; set; } = "http://localhost:80";

        public Task<List<Statistic>> GetStatisticsAsync()
        {
            var rnd = new Random();
            _requests = new Dictionary<(string type, string name), int>();
            _fails = new Dictionary<(string type, string name), int>();

            var statistics = Enumerable.Range(1, 5)
                .Select(x =>
                {
                    var type = httpTypes[rnd.Next(httpTypes.Length)];
                    var name = httpNames[rnd.Next(httpNames.Length)];
                    var req = rnd.Next(x, 20000);
                    _requests.Add((type, name), req);
                    var fail = rnd.Next(0, 1000);
                    _fails.Add((type, name), fail);

                    var first = rnd.Next(50, 80);
                    var second = 100 - rnd.Next(81, 90);
                    var third = 100 - rnd.Next(91, 95);
                    var reqFirst = req / 100 * first;
                    var reqSecond = req / 100 * second;
                    var reqThird = req / 100 * third;
                    var reqLast = req - reqFirst - reqSecond - reqThird > 0 
                        ? req - reqFirst - reqSecond - reqThird
                        : 0;
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

                    return new Statistic
                    {
                        Type = type,
                        Name = name,
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
                    };
                })
                .OrderBy(x => x.Name)
                .ToList();
            
            // add aggregate
            statistics.Add(AggregateStatistics(statistics));

            return Task.FromResult(statistics);
        }

        public Task<Failure[]> GetFailuresAsync()
        {
            var fails = _fails.Select(x => new Failure
            {
                Fails = x.Value,
                Method = x.Key.type,
                Name = x.Key.name,
                Type = new HttpRequestException($"404 Client Error. NOT FOUND for url: {HostAddress}{x.Key.name}"),
            })
            .ToArray();
            return Task.FromResult(fails);
        }

        /// <summary>
        /// Calculate aggregated statistics
        /// </summary>
        /// <param name="statistics"></param>
        /// <returns></returns>
        private Statistic AggregateStatistics(List<Statistic> statistics)
        {
            return new Statistic
            {
                Type = "",
                Name = "Aggregated",
                Requests = statistics.Sum(x => x.Requests),
                Fails = statistics.Sum(x => x.Fails),
                // memo: omit aggregated data calculations
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
        }
    }
}
