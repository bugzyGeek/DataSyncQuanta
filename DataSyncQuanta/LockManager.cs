using System.Collections.Concurrent;

namespace DataSyncQuanta;

/// <summary>
/// Manages locks for transactions to ensure data consistency and handle deadlocks.
/// </summary>
/// <typeparam name="TKey">The type of the key used to identify locks.</typeparam>
public class LockManager<TKey> : IDisposable where TKey : notnull
{
    private readonly ConcurrentDictionary<TKey, LockInfo> _locks = new();
    private readonly TimeSpan _expirationTime;
    private readonly TimeSpan _timeout;
    private readonly TimeSpan _maxLockDuration;
    private readonly DeadlockResolutionStrategy? _deadlockResolutionStrategy;
    private readonly Timer _evictionTimer;
    private readonly Timer _deadlockDetectionTimer;
    private readonly DeadlockGraph _deadlockGraph = new();
    private bool _disposed = false;
    private readonly object _disposeLock = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="LockManager{TKey}"/> class.
    /// </summary>
    /// <param name="expirationTime">The time after which a lock expires if not accessed.</param>
    /// <param name="timeout">The time to wait for acquiring a lock before timing out.</param>
    /// <param name="maxLockDuration">The maximum duration a lock can be held.</param>
    /// <param name="evictionInterval">The interval at which expired locks are evicted.</param>
    /// <param name="deadlockDetectionInterval">The interval at which deadlocks are detected.</param>
    /// <param name="deadlockResolutionStrategy">The strategy to resolve deadlocks.</param>
    public LockManager(TimeSpan? expirationTime = null, TimeSpan? timeout = null, TimeSpan? maxLockDuration = null, TimeSpan? evictionInterval = null, TimeSpan? deadlockDetectionInterval = null, DeadlockResolutionStrategy? deadlockResolutionStrategy = null)
    {
        _expirationTime = expirationTime ?? GlobalLockManagerConfig.LockManagerConfig.ExpirationTime;
        _timeout = timeout ?? GlobalLockManagerConfig.LockManagerConfig.Timeout;
        _maxLockDuration = maxLockDuration ?? GlobalLockManagerConfig.LockManagerConfig.MaxLockDuration;
        _deadlockResolutionStrategy = deadlockResolutionStrategy ?? GlobalLockManagerConfig.LockManagerConfig.DeadlockResolutionStrategy;
        _evictionTimer = new Timer(EvictExpiredLocks, null, evictionInterval ?? GlobalLockManagerConfig.LockManagerConfig.EvictionInterval, evictionInterval ?? GlobalLockManagerConfig.LockManagerConfig.EvictionInterval);
        _deadlockDetectionTimer = new Timer(DetectDeadlocks, null, deadlockDetectionInterval ?? GlobalLockManagerConfig.LockManagerConfig.DeadlockDetectionInterval, deadlockDetectionInterval ?? GlobalLockManagerConfig.LockManagerConfig.DeadlockDetectionInterval);
    }

    /// <summary>
    /// Evicts expired locks from the lock manager.
    /// </summary>
    /// <param name="state">The state object passed to the timer callback.</param>
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

    /// <summary>
    /// Gets the lock information for the specified key.
    /// </summary>
    /// <param name="key">The key for which to get the lock information.</param>
    /// <returns>The lock information for the specified key.</returns>
    private LockInfo GetLockInfo(TKey key)
    {
        return _locks.GetOrAdd(key, _ => new LockInfo());
    }

    /// <summary>
    /// Tries to start a transaction for the specified key.
    /// </summary>
    /// <param name="key">The key for which to start the transaction.</param>
    /// <returns><c>true</c> if the transaction was started successfully; otherwise, <c>false</c>.</returns>
    public bool TryStartTransaction(TKey key)
    {
        return TryStartTransaction(key, _timeout);
    }

    /// <summary>
    /// Tries to start a transaction for the specified key with a specified timeout.
    /// </summary>
    /// <param name="key">The key for which to start the transaction.</param>
    /// <param name="timeout">The timeout for acquiring the lock.</param>
    /// <returns><c>true</c> if the transaction was started successfully; otherwise, <c>false</c>.</returns>
    public bool TryStartTransaction(TKey key, TimeSpan timeout)
    {
        var lockInfo = GetLockInfo(key);
        bool lockAcquired = Monitor.TryEnter(lockInfo.LockObject, timeout);
        if (lockAcquired)
        {
            lockInfo.LastAccessed = DateTime.UtcNow;
            lockInfo.LockAcquiredTime = DateTime.UtcNow;
            lockInfo.IsLocked = true;
            lockInfo.ReleaseTimer = new Timer(ReleaseLock, key, _maxLockDuration, Timeout.InfiniteTimeSpan);
            _deadlockGraph.AddEdge(key.ToString(), "Resource");
            if (GlobalLockManagerConfig.EnableLogging)
            {
                Console.WriteLine($"Lock acquired for key: {key} at {lockInfo.LockAcquiredTime}");
            }
        }
        else
        {
            _deadlockGraph.AddEdge("Transaction", key.ToString());
        }
        return lockAcquired;
    }

    /// <summary>
    /// Ends the transaction for the specified key.
    /// </summary>
    /// <param name="key">The key for which to end the transaction.</param>
    public void EndTransaction(TKey key)
    {
        var lockInfo = GetLockInfo(key);
        if (Monitor.IsEntered(lockInfo.LockObject))
        {
            Monitor.Exit(lockInfo.LockObject);
            lockInfo.IsLocked = false;
            lockInfo.ReleaseTimer?.Dispose();
            _deadlockGraph.RemoveEdge(key.ToString(), "Resource");
            if (GlobalLockManagerConfig.EnableLogging)
            {
                Console.WriteLine($"Lock released for key: {key} at {DateTime.UtcNow}");
            }
        }
    }

    /// <summary>
    /// Releases the lock for the specified key.
    /// </summary>
    /// <param name="state">The state object passed to the timer callback.</param>
    private void ReleaseLock(object? state)
    {
        if (state is TKey key)
        {
            EndTransaction(key);
            if (GlobalLockManagerConfig.EnableLogging)
            {
                Console.WriteLine($"Lock automatically released for key: {key} after max duration");
            }
            throw new TimeoutException("Transaction timed out.");
        }
    }

    /// <summary>
    /// Detects and resolves deadlocks by terminating one of the transactions involved in the deadlock.
    /// </summary>
    /// <param name="state">The state object passed to the timer callback.</param>
    private void DetectDeadlocks(object? state)
    {
        if (_deadlockGraph.HasCycle())
        {
            if (GlobalLockManagerConfig.EnableLogging)
            {
                Console.WriteLine("Deadlock detected!");
            }

            var cycleNodes = _deadlockGraph.GetCycleNodes();
            if (cycleNodes != null && cycleNodes.Any())
            {
                string nodeToTerminate = cycleNodes.First();

                if (_deadlockResolutionStrategy == DeadlockResolutionStrategy.TerminateOldest)
                {
                    nodeToTerminate = cycleNodes.OrderBy(node => GetLockInfo((TKey)Convert.ChangeType(node, typeof(TKey))).LockAcquiredTime).First();
                }
                else if (_deadlockResolutionStrategy == DeadlockResolutionStrategy.TerminateNewest)
                {
                    nodeToTerminate = cycleNodes.OrderByDescending(node => GetLockInfo((TKey)Convert.ChangeType(node, typeof(TKey))).LockAcquiredTime).First();
                }

                if (nodeToTerminate != null)
                {
                    if (GlobalLockManagerConfig.EnableLogging)
                    {
                        Console.WriteLine($"Terminating transaction for key: {nodeToTerminate} to resolve deadlock.");
                    }

                    EndTransaction((TKey)Convert.ChangeType(nodeToTerminate, typeof(TKey)));
                    throw new TransactionTerminatedException($"Transaction for key: {nodeToTerminate} was terminated to resolve a deadlock.");
                }
            }
        }
    }

    /// <summary>
    /// Disposes the resources used by the <see cref="LockManager{TKey}"/>.
    /// </summary>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Disposes the resources used by the <see cref="LockManager{TKey}"/>.
    /// </summary>
    /// <param name="disposing">A value indicating whether the method was called from the Dispose method.</param>
    protected virtual void Dispose(bool disposing)
    {
        if (_disposed)
            return;

        lock (_disposeLock) // Use the lock to ensure thread safety
        {
            if (_disposed)
                return;
            if (disposing)
            {
                _evictionTimer.Dispose();
                _deadlockDetectionTimer.Dispose();
            }
            _disposed = true;
        }
    }
}
