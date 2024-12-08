namespace DataSyncQuanta;


/// <summary>
/// Configuration settings for the LockManager.
/// </summary>
public static class GlobalLockManagerConfig
{
    /// <summary>
    /// Gets or sets the configuration for the LockManager.
    /// </summary>
    public static LockManagerConfig LockManagerConfig { get; set; } = new LockManagerConfig();

    /// <summary>
    /// Gets or sets a value indicating whether logging is enabled.
    /// </summary>
    public static bool EnableLogging { get; set; } = true;
}

/// <summary>
/// Specifies the strategy for resolving deadlocks.
/// </summary>
public enum DeadlockResolutionStrategy
{
    /// <summary>
    /// Terminate the oldest transaction involved in the deadlock.
    /// </summary>
    TerminateOldest,

    /// <summary>
    /// Terminate the newest transaction involved in the deadlock.
    /// </summary>
    TerminateNewest
}


/// <summary>
/// Configuration settings for the LockManager.
/// </summary>
public class LockManagerConfig
{
    /// <summary>
    /// Gets or sets the expiration time for locks.
    /// </summary>
    public TimeSpan ExpirationTime { get; set; } = TimeSpan.FromMinutes(5);

    /// <summary>
    /// Gets or sets the timeout duration for acquiring locks.
    /// </summary>
    public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(30);

    /// <summary>
    /// Gets or sets the maximum duration a lock can be held before it is automatically released.
    /// </summary>
    public TimeSpan MaxLockDuration { get; set; } = TimeSpan.FromMinutes(1);

    /// <summary>
    /// Gets or sets the interval at which expired locks are evicted.
    /// </summary>
    public TimeSpan EvictionInterval { get; set; } = TimeSpan.FromMinutes(1);

    /// <summary>
    /// Gets or sets the interval at which deadlock detection is performed.
    /// </summary>
    public TimeSpan DeadlockDetectionInterval { get; set; } = TimeSpan.FromSeconds(10);

    /// <summary>
    /// Gets or sets the strategy for resolving deadlocks.
    /// </summary>
    public DeadlockResolutionStrategy DeadlockResolutionStrategy { get; set; } = DeadlockResolutionStrategy.TerminateOldest;
}