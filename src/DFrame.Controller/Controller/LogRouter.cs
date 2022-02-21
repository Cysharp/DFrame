using ObservableCollections;

namespace DFrame.Controller
{
    public class LogRouter
    {
        ObservableFixedSizeRingBuffer<string> ringBuffer;

        public LogRouter()
        {
            ringBuffer = new ObservableFixedSizeRingBuffer<string>(1000); // global log buffer.
        }

        public ISynchronizedView<string, string> GetView()
        {
            return ringBuffer.CreateView<string>(x => x);
        }

        public void Add(string s)
        {
            ringBuffer.AddLast(s);
        }
    }

    public class RoutingLoggerProvider : ILoggerProvider
    {
        readonly LogRouter router;

        public RoutingLoggerProvider(LogRouter router)
        {
            this.router = router;
        }

        public ILogger CreateLogger(string categoryName)
        {
            return new RoutingLogger(router);
        }

        public void Dispose()
        {
        }
    }

    public class RoutingLogger : ILogger
    {
        readonly LogRouter router;

        public RoutingLogger(LogRouter router)
        {
            this.router = router;
        }

        public IDisposable BeginScope<TState>(TState state)
        {
            return NilDisposable.Instance;
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            return true;
        }

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            router.Add(formatter(state, exception));
        }

        class NilDisposable : IDisposable
        {
            public static readonly IDisposable Instance = new NilDisposable();

            public void Dispose()
            {

            }
        }
    }
}