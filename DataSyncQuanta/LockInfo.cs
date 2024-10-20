namespace DataSyncQuanta;
internal class LockInfo
{
    public object LockObject { get; } = new object();
    public DateTime LastAccessed { get; set; } = DateTime.UtcNow;
    public bool IsLocked { get; set; }
}