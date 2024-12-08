using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataSyncQuanta.Collection.Generic
{
    public class DataSyncList<T> where T : notnull
    {
        private readonly SyncList<T> _list;

        public DataSyncList(TimeSpan? expirationTime = null, TimeSpan? timeout = null, TimeSpan? maxLockDuration = null, TimeSpan? evictionInterval = null, TimeSpan? deadlockDetectionInterval = null, DeadlockResolutionStrategy? deadlockResolutionStrategy = null)
        {
            _list = new SyncList<T>(expirationTime, timeout, maxLockDuration, evictionInterval, deadlockDetectionInterval, deadlockResolutionStrategy);
        }

        public ISyncList<T> Transaction()
        {
            return _list;
        }

        public Dictionary<int, bool> GetAcquiredLocks()
        {
            return _list.GetAcquiredLocks();
        }
    }
}
