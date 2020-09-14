using Microsoft.Extensions.DependencyInjection;

namespace DFrame.Profiler
{
    public static class DFrameProfilerServiceCollectionExtensions
    {
        public static IServiceCollection AddDFrameProfiler(this IServiceCollection services, DFrameProfilerOption option)
        {
            services.AddSingleton<DFrameProfilerOption>(option);
            services.AddScoped<IDFrameProfiler, DFrameProfiler>();
            return services;
        }
    }
}
