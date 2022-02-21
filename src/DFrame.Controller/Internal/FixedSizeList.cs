using ObservableCollections;

namespace DFrame.Internal
{
    internal class FixedSizeList<T>
    {
        readonly RingBuffer<T> buffer;
        readonly int fixedSize;

        public FixedSizeList(int fixedSize)
        {
            this.buffer = new RingBuffer<T>();
            this.fixedSize = fixedSize;
        }

        public void AddLast(T item)
        {
            if (fixedSize == buffer.Count)
            {
                buffer.RemoveFirst();
            }

            buffer.AddLast(item);
        }

        public T[] ToArray()
        {
            return buffer.ToArray();
        }
    }
}