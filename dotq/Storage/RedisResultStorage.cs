using System;
using System.Threading;
using dotq.Storage.RedisStructures;
using dotq.Task;
using dotq.TaskResultHandle;
using ServiceStack;
using ServiceStack.Redis;
using StackExchange.Redis;

namespace dotq.Storage
{

    public static class ResultStoreConstants
    {
        public static string TaskResults = "TaskResults";
    }

    public class SimpleRedisStore : IDataStore
    {
        private ConnectionMultiplexer _redis;
        private IDatabase _redisDatabase;
        private RedisHash _hash;

        public SimpleRedisStore(ConnectionMultiplexer redis)
        {
            _redis = redis;
            _redisDatabase = redis.GetDatabase();
            _hash = new RedisHash(_redisDatabase, ResultStoreConstants.TaskResults);
        }

        public void Clear()
        {
            throw new NotImplementedException();
        }

        public void PutData(string key, object value)
        {
            _hash[key] = (string)value;
        }

        public object GetData(string key)
        {
            return _hash[key];
        }

        public object PopData(string key)
        {
            if (_hash.In(key))
                return _hash[key];
            return null;
        }

        public bool In(string key)
        {
            return _hash.In(key);
        }
    }
}