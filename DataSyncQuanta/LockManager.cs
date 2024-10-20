using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;

namespace DataSyncQuanta
{
    public partial class LockManager<TKey>
    {
        private readonly ConcurrentDictionary<DataSourceKey<TKey>, LockInfo> _locks = new ConcurrentDictionary<DataSourceKey<TKey>, LockInfo>();
        private readonly TimeSpan _expirationTime;
        private readonly Timer _evictionTimer;

        public LockManager(TimeSpan expirationTime)
        {
            _expirationTime = expirationTime;
            _evictionTimer = new Timer(EvictExpiredLocks, null, _expirationTime, _expirationTime);
        }

        private void EvictExpiredLocks(object? state)
        {
            var now = DateTime.UtcNow;
            var expiredKeys = _locks.Where(kvp => (now - kvp.Value.LastAccessed) > _expirationTime && !kvp.Value.IsLocked)
                                    .Select(kvp => kvp.Key)
                                    .ToList();

            foreach (var key in expiredKeys)
            {
                _locks.TryRemove(key, out _);
            }
        }

        private LockInfo GetLockInfo(DataSourceKey<TKey> key)
        {
            return _locks.GetOrAdd(key, _ => new LockInfo());
        }

        public void StartTransaction(DataSourceKey<TKey> key)
        {
            var lockInfo = GetLockInfo(key);
            Monitor.Enter(lockInfo.LockObject);
            lockInfo.LastAccessed = DateTime.UtcNow;
            lockInfo.IsLocked = true;
        }

        public void EndTransaction(DataSourceKey<TKey> key)
        {
            var lockInfo = GetLockInfo(key);
            Monitor.Exit(lockInfo.LockObject);
            lockInfo.IsLocked = false;
        }
    }
}
