using Microsoft.Extensions.DependencyInjection;

namespace DFrame.Controller
{
    // TODO:remove this???

    internal static class GlobalServiceProvider
    {
        public static IServiceProvider Instance { get; set; } = default!;

        public static ILogger<T> GetLogger<T>() => Instance.GetRequiredService<ILogger<T>>();
    }
}
