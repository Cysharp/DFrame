using System.Collections.Concurrent;

namespace DFrame.Internal
{
    public class KeyedValueProvider<T>
         where T : new()
    {
        readonly ConcurrentDictionary<string, T> dictionary;

        public KeyedValueProvider()
        {
            this.dictionary = new ConcurrentDictionary<string, T>();
        }

        public T GetValue(string key)
        {
            return dictionary.GetOrAdd(key, _ => new T());
        }
    }
}
