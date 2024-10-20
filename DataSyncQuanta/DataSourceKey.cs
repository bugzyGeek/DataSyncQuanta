namespace DataSyncQuanta
{
    public class DataSourceKey<TKey>
    {
        public string DataSource { get; }
        public TKey Key { get; }

        public DataSourceKey(string dataSource, TKey key)
        {
            DataSource = dataSource;
            Key = key;
        }

        public override bool Equals(object obj)
        {
            if (obj is DataSourceKey<TKey> other)
            {
                return DataSource == other.DataSource && EqualityComparer<TKey>.Default.Equals(Key, other.Key);
            }
            return false;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(DataSource, Key);
        }
    }
}
