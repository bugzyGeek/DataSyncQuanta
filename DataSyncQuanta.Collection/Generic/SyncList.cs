using System.Collections;

namespace DataSyncQuanta.Collection.Generic
{
    internal class SyncList<T> : IList<T>, IDisposable, ISyncList<T>
    {
        public readonly List<T> _list = new();
        public readonly LockManager<int> _lockManager;
        private readonly Dictionary<int, bool> _acquiredLocks = new();
        private readonly Dictionary<int, HashSet<int>> _transactionLocks = new();
        private readonly object _lock = new();

        public SyncList(TimeSpan? expirationTime = null, TimeSpan? timeout = null, TimeSpan? maxLockDuration = null, TimeSpan? evictionInterval = null, TimeSpan? deadlockDetectionInterval = null, DeadlockResolutionStrategy? deadlockResolutionStrategy = null)
        {
            _lockManager = new LockManager<int>(expirationTime, timeout, maxLockDuration, evictionInterval, deadlockDetectionInterval, deadlockResolutionStrategy);
        }

        public T this[int index]
        {
            get
            {
                AcquireLock(index);
                return _list[index];
            }
            set
            {
                AcquireLock(index);
                _list[index] = value;
            }
        }

        public int Count => _list.Count;
        public bool IsReadOnly => false;

        public void Add(T item)
        {
            _list.Add(item);
        }

        public void Clear()
        {
            _list.Clear();
            _acquiredLocks.Clear();
        }

        public bool Contains(T item)
        {
            return _list.Contains(item);
        }

        public void CopyTo(T[] array, int arrayIndex)
        {
            _list.CopyTo(array, arrayIndex);
        }

        public IEnumerator<T> GetEnumerator()
        {
            return _list.GetEnumerator();
        }

        public int IndexOf(T item)
        {
            return _list.IndexOf(item);
        }

        public void Insert(int index, T item)
        {
            AcquireLock(index);
            _list.Insert(index, item);
        }

        public bool Remove(T item)
        {
            var index = _list.IndexOf(item);
            if (index == -1)
                return false;
            AcquireLock(index);
            return _list.Remove(item);
        }

        public void RemoveAt(int index)
        {
            AcquireLock(index);
            _list.RemoveAt(index);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        private void AcquireLock(int index)
        {
            var acquiredLock = _lockManager.TryStartTransaction(index);
            if (!acquiredLock)
                throw new InvalidOperationException("Could not acquire lock");

            lock (_lock)
            {
                _acquiredLocks[index] = true;
                if (!_transactionLocks.TryGetValue(Environment.CurrentManagedThreadId, out var locks))
                {
                    locks = new HashSet<int>();
                    _transactionLocks[Environment.CurrentManagedThreadId] = locks;
                }
                locks.Add(index);
            }
        }

        public Dictionary<int, bool> GetAcquiredLocks()
        {
            return new Dictionary<int, bool>(_acquiredLocks);
        }

        public void Dispose()
        {
            lock (_lock)
            {
                if (_transactionLocks.TryGetValue(Environment.CurrentManagedThreadId, out var locks))
                {
                    foreach (var index in locks)
                    {
                        _lockManager.EndTransaction(index);
                        _acquiredLocks.Remove(index);
                    }
                    _transactionLocks.Remove(Environment.CurrentManagedThreadId);
                }
            }
        }
    }
}
