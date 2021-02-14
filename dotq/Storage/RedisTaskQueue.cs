using System;
using System.Collections.Generic;
using dotq.Storage.RedisStructures;
using StackExchange.Redis;
using System.Threading.Tasks;
using dotq.Task;

namespace dotq.Storage
{
    
    public static class RedisTaskQueueConstants
    {
        public static string QueueKey = "TaskQueue";
    }


    [Serializable]
    class QueueIsEmptyException : Exception
    {
        public QueueIsEmptyException() {}

        public QueueIsEmptyException(string name) {}
  
    }
    
    
    public class RedisTaskQueue : IDotQueue<ITask>
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

        public void Enqueue(ITask t, int? priority = null)
        {
            _list.Lpush(t.Serialize());
        }
        
        public string DequeueString()
        {
            return _list.Rpop();
        }
        
        public ITask Dequeue()
        {
            var deserializer = new DefaultTaskDeserializer();
            var obj = _list.Rpop();
            if (obj.IsNull)
                throw new QueueIsEmptyException();

            return deserializer.Deserialize(obj.ToString());
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

        public bool IsEmpty()
        {
            return _list.IsEmpty();
        }

        public IList<RedisValue> ToList()
        {
            return _list.Range();
        }
        
        
    }
}