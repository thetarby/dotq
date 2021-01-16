using System;
using System.Collections.Generic;
using dotq.Storage.RedisStructures;
using StackExchange.Redis;
using System.Threading.Tasks;

namespace dotq.Storage
{
    
    public static class RedisTaskQueueConstants
    {
        public static string QueueKey = "TaskQueue";
    }
    
    public class RedisTaskQueue : ITaskQueue<string>
    {
        private ConnectionMultiplexer _redis;
        private RedisList _list;

        public RedisTaskQueue(ConnectionMultiplexer redis)
        {
            _redis = redis;
            var db=redis.GetDatabase();
            _list = new RedisList(db, RedisTaskQueueConstants.QueueKey);
        }
        
        public void Enqueue(string o, int? priority = null)
        {
            _list.Lpush(o);
        }

        public string Dequeue()
        {
            return _list.Rpop();
        }
        
        /// <summary>
        /// Blocking dequeue. If queue is empty wait until it is filled. 
        /// </summary>
        /// <returns></returns>
        public Task<RedisValue> BDequeue()
        {
            try
            {
                return _list.BRpop();
            }
            catch (Exception e)
            {
                // there might be a timeout error
                // TODO: what to do in a timeout
                Console.WriteLine(e);
                throw;
            }
        }

        public long Length()
        {
            return _list.Length;
        }

        public void Clear()
        {
            _list.Destroy();
        }

        public IList<RedisValue> ToList()
        {
            return _list.Range();
        }
        
        
    }
}