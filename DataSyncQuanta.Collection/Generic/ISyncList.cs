
namespace DataSyncQuanta.Collection.Generic
{
    internal interface ISyncList<T>
    {
        T this[int index] { get; set; }

        int Count { get; }
        bool IsReadOnly { get; }

        void Add(T item);
        void Clear();
        bool Contains(T item);
        void CopyTo(T[] array, int arrayIndex);
        void Dispose();
        Dictionary<int, bool> GetAcquiredLocks();
        IEnumerator<T> GetEnumerator();
        int IndexOf(T item);
        void Insert(int index, T item);
        bool Remove(T item);
        void RemoveAt(int index);
    }
}