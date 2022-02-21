using ObservableCollections;

namespace DFrame.Controller
{
    // singleton.
    public class DFrameControllerLogBuffer
    {
        ObservableFixedSizeRingBuffer<string> ringBuffer;

        public DFrameControllerLogBuffer(DFrameControllerOptions options)
        {
            ringBuffer = new ObservableFixedSizeRingBuffer<string>(options.ServerLogBufferCount);
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

    public class DFrameControllerLoggerProvider : ILoggerProvider
    {
        readonly DFrameControllerLogBuffer router;

        public DFrameControllerLoggerProvider(DFrameControllerLogBuffer router)
        {
            this.router = router;
        }

        public ILogger CreateLogger(string categoryName)
        {
            return new DFrameControllerLogger(router);
        }

        public void Dispose()
        {
        }
    }

    public class DFrameControllerLogger : ILogger
    {
        readonly DFrameControllerLogBuffer router;

        public DFrameControllerLogger(DFrameControllerLogBuffer router)
        {
            this.router = router;
        }

        public IDisposable BeginScope<TState>(TState state)
        {
            return NilDisposable.Instance;
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            return logLevel != LogLevel.None;
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