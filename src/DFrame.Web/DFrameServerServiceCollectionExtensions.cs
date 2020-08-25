using Cysharp.Text;
using DFrame.Web.Infrastructure;
using DFrame.Web.Models;
using Microsoft.Extensions.DependencyInjection;
using ZLogger;

namespace DFrame.Web
{
    public static class DFrameServerServiceCollectionExtensions
    {
        public static IServiceCollection AddDFrameWeb(this IServiceCollection services)
        {
            services.AddSingleton<ExecuteService>();
            services.AddSingleton<IStatisticsService, StatisticsMockService>();
            services.AddSingleton<IWorkersService, WorkerMockService>();
            services.AddSingleton<ISummaryService, SummaryService>();
            services.AddSingleton<ILoggingService, LoggingService>();
            services.AddSingleton<LogProcessorOptions>(new LogProcessorOptions()
            {
                LoggerOptions = new ZLoggerOptions
                {
                    ExceptionFormatter = (writer, ex) => ZString.Utf8Format(writer, "{0} {1}", ex.Message, ex.StackTrace),
                },
            });
            return services;
        }
    }
}
