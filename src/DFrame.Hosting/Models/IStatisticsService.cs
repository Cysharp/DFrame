using DFrame.Hosting.Data;
using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace DFrame.Hosting.Models
{
    public static class MockData
    {
        public static readonly string[] HttpTypes = new[]
        {
            "Get", "Patch", "Post", "Put", "Delete",
        };

        public static readonly string[] Paths = new[]
        {
            "/",
            "/Hello", "/Item", "/World",
            "/Hoge", "/Fuga", "/Piyo", "/Foo", "/Bar",
            "/Logout", "/Login", "/Auth", "/Register",
            "/Healthz", "/Liveness", "/Readiness", "/Stats",
            "/Begin", "/Questions", "/Faq", "/Post", "/Tasks", "/Cards", "/Display", "/Report",
        };
    }
    public interface IStatisticsService
    {
        event Action<Statistic> OnUpdateStatistics;

        void RegisterContext(IExecuteContext executeContext);
        /// <summary>
        /// Get statistics
        /// </summary>
        /// <returns></returns>
        Task<(Statistic[] statistics, Statistic aggregated)> GetStatisticsAsync();
    }

    // todo: prepare StatisticService. Get DFrame Statistic data.
}
