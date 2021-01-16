using StackExchange.Redis;
using System.Threading.Tasks;

namespace dotq.Storage.RedisStructures
{
    /// <summary>
    /// very simple class which simplifies hash operations with stack exchange redis client
    /// </summary>
    public class RedisHash
    {
        private IDatabase _redisDb;
        public string HashKey { get;}

        public RedisHash(IDatabase redisDb, string hashKey)
        {
            _redisDb = redisDb;
            this.HashKey = hashKey;
        }
        
        public RedisValue this[string key]
        {
            get => _redisDb.HashGet(HashKey, key);
            set => _redisDb.HashSet(HashKey, key, value);
        }

        public bool In(string key) => _redisDb.HashExists(HashKey, key);
        
        public HashEntry[] All => _redisDb.HashGetAll(HashKey);
        
        public long Length => _redisDb.HashLength(HashKey);
        
        
    }

    public class RedisList
    {
        private IDatabase _redisDb;
        public string ListKey { get;}

        public RedisList(IDatabase redisDb, string listKey)
        {
            _redisDb = redisDb;
            this.ListKey = listKey;
        }
        
        public long Length => _redisDb.ListLength(ListKey);
        public RedisValue Lpop() => _redisDb.ListLeftPop(ListKey);
        public Task<RedisValue> BLpop() => _redisDb.ListLeftPopAsync(ListKey);
        public long Lpush(RedisValue value) => _redisDb.ListLeftPush(ListKey, value);
        public RedisValue Rpop() => _redisDb.ListRightPop(ListKey);
        public Task<RedisValue> BRpop() => _redisDb.ListRightPopAsync(ListKey);
        
        public long Rpush(RedisValue value) => _redisDb.ListRightPush(ListKey, value);
        public RedisValue[] Range(long start=0, long end=-1) => _redisDb.ListRange(ListKey, start, end);
        
        public RedisValue this[long key]
        {
            get => _redisDb.ListGetByIndex(ListKey, key);
            set => _redisDb.ListSetByIndex(ListKey, key, value);
        }

        public bool Destroy() => _redisDb.KeyDelete(ListKey);
    }
}