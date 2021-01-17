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

    public class SimpleRedisTaskResultStore : ITaskResultStore<string>
    {
        private ConnectionMultiplexer _redis;
        private IDatabase _redisDatabase;
        private RedisHash _hash;
        private Func<ITaskDeserializer> GetTaskDeserializer; 

        public SimpleRedisTaskResultStore(ConnectionMultiplexer redis)
        {
            _redis = redis;
            _redisDatabase = redis.GetDatabase();
            _hash = new RedisHash(_redisDatabase, ResultStoreConstants.TaskResults);
            GetTaskDeserializer = () => new DefaultTaskDeserializer();
        }

        public void Clear()
        {
            _hash.Clear();
        }

        public void PutData(string key, string value)
        {
            _hash[key] = value;
        }

        public string GetData(string key)
        {
            return _hash[key];
        }

        public string PopData(string key)
        {
            throw new NotImplementedException();
        }

        public bool In(string key)
        {
            return _hash.In(key);
        }

        public string GetResultOfTask(ITask t)
        {
            var obj = _hash[t.GetInstanceIdentifier()];
            return obj.ToString();
        }

        public string GetResultOfTask(string taskInstanceId)
        {
            var obj = _hash[taskInstanceId];
            return obj.ToString();
        }

        public bool SetResultOfTask(ITask t)
        {
            var deserializer = GetTaskDeserializer.Invoke();
            if (t.IsExecuted())
            {
                var res= t.SerializeResult();
                _hash[t.GetInstanceIdentifier()] = res;
            }
            else
            {
                t.Execute();
                _hash[t.GetInstanceIdentifier()] = t.SerializeResult();                
            }

            //TODO: return success
            return true;
        }

        public bool SetResultOfTask(ITask t, string result)
        {
            _hash[t.GetInstanceIdentifier()] = t.SerializeResult();
            return true;
        }

        public bool SetResultOfTask(string taskInstanceId, string result)
        { 
            _hash[taskInstanceId] = result;
            return true;
        }
    }
}