using MessagePack;
using MessagePack.Formatters;

namespace DFrame
{
    internal class DFrameResolver : IFormatterResolver
    {
        internal static IFormatterResolver Instance = new DFrameResolver();
        internal static MessagePack.MessagePackSerializerOptions Options = MessagePackSerializerOptions.Standard.WithResolver(Instance);

        DFrameResolver()
        {

        }

        public IMessagePackFormatter<T> GetFormatter<T>()
        {
            return Cache<T>.Formatter;
        }

        static class Cache<T>
        {
            public static readonly IMessagePackFormatter<T> Formatter;

            static Cache()
            {
                var f = DFrame.Resolvers.MagicOnionResolver.Instance.GetFormatter<T>();
                if (f != null)
                {
                    Formatter = f;
                    return;
                }

                f = DFrame.Resolvers.GeneratedResolver.Instance.GetFormatter<T>();
                if (f != null)
                {
                    Formatter = f;
                    return;
                }

                f = MessagePack.Resolvers.StandardResolver.Instance.GetFormatter<T>();
                if (f != null)
                {
                    Formatter = f;
                    return;
                }
            }
        }
    }
}
