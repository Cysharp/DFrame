using Microsoft.Extensions.DependencyInjection;

namespace DFrame.Controller
{
    internal static class GlobalServiceProvider
    {
        public static IServiceProvider Instance { get; set; } = default!;

        public static ILogger<T> GetLogger<T>() => Instance.GetRequiredService<ILogger<T>>();
    }
}
