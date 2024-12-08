using StackExchange.Redis;

namespace DataSyncQuanta.Redis
{
    public class RedisDataSync : LockManager<RedisKey>
    {
        private readonly IDatabase _database;
        private readonly int _databaseIndex;

        internal RedisDataSync(IDatabase database, int databaseIndex)
        {
            _database = database;
            _databaseIndex = databaseIndex;
        }


    }
}
