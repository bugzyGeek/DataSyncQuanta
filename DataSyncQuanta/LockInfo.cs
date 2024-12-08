namespace DataSyncQuanta;
/// <summary>
/// Represents information about a lock, including its state and timing details.
/// </summary>
internal class LockInfo
{
    /// <summary>
    /// Gets the object used for locking.
    /// </summary>
    public object LockObject { get; } = new object();

    /// <summary>
    /// Gets or sets the last time the lock was accessed.
    /// </summary>
    public DateTime LastAccessed { get; set; }

    /// <summary>
    /// Gets or sets the time when the lock was acquired.
    /// </summary>
    public DateTime LockAcquiredTime { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the lock is currently held.
    /// </summary>
    public bool IsLocked { get; set; }

    /// <summary>
    /// Gets or sets the timer used to release the lock after a maximum duration.
    /// </summary>
    public Timer? ReleaseTimer { get; set; }
}
