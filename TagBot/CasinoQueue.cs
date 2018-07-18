using System.Collections.Concurrent;

namespace TagBot
{
    public class CasinoQueue<T> : ConcurrentQueue<T>
    {
        private readonly int _size;

        public CasinoQueue(int size)
        {
            _size = size;
        }

        public new void Enqueue(T item)
        {
            if (Count == _size)
                TryDequeue(out _);
            base.Enqueue(item);
        }
    }
}