using System;
using System.Threading;
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
    
    public class SimpleRedisResultStorage : IResultStorage
    {
        private ConnectionMultiplexer _redis;
        private IDatabase _redisDatabase;
        
        public SimpleRedisResultStorage(ConnectionMultiplexer redis)
        {
            _redis = redis;
            _redisDatabase = redis.GetDatabase();
        }
        

        public ITaskResultHandle GetResultOfTask(ITask t)
        {
            throw new NotImplementedException();
        }

        
        public ITaskResultHandle GetResultOfTask(string taskInstanceId)
        {
            throw new NotImplementedException();
        }

        
        public object GetRawResult(string taskInstanceId)
        {
            return _redisDatabase.HashGet(ResultStoreConstants.TaskResults, taskInstanceId);
        }

        
        public object GetRawResult(ITask t)
        {
            return _redisDatabase.HashGet(ResultStoreConstants.TaskResults, t.GetIdentifier());
        }

        
        public bool SetResultOfTask(ITask t)
        {
            _redisDatabase.HashSet(ResultStoreConstants.TaskResults, t.GetIdentifier(), t.SerializeResult());
            return true;
        }
    }
}