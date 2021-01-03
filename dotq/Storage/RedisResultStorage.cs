using System;
using System.Threading;
using dotq.Task;
using dotq.TaskResultHandle;
using ServiceStack.Redis;

namespace dotq.Storage
{
    public class RedisResultStorage : IResultStorage
    {
        private IRedisClientsManager _redisClientsManager;

        public RedisResultStorage(IRedisClientsManager redisClientsManager)
        {
            _redisClientsManager = redisClientsManager;
        }

        private void PromiseLikeRedisPubSub(string waitedChannel, Action<string, string> onMessage, float promiseTimeoutInSeconds=-1)
        {
            
            // lock which prevents CloseRedisPubSub thread to close a connection
            // when onmessage handler is called this lock is released and the thread that calls
            // CloseRedisPubSub will close the connection
            Semaphore luck = new Semaphore(0,1);
            
            // subscribe to the channel with the name task_id+task_creation_time which is spesific to a task instance
            // creation time might not be unique hence it should be changed
            var redisPubSub = new RedisPubSubServer(_redisClientsManager, waitedChannel)
            {
                OnMessage = (channel, msg) =>
                {
                    onMessage(channel, msg);
                    luck.Release();
                },
            };
            
            var obj=(redisPubSub, luck, promiseTimeoutInSeconds);
            
            // start thread which will close the connection after acquiring the lock
            Thread thread = new Thread(new ParameterizedThreadStart(CloseRedisPubSub));
            thread.Start(obj);
            redisPubSub.Start();
        }
        
        private void CloseRedisPubSub(object o)
        {
            var args = ((RedisPubSubServer, Semaphore, float)) o;
            var promiseTimeoutInSeconds = args.Item3;
            var l = args.Item2;
            var c = args.Item1;
            
            if (promiseTimeoutInSeconds == -1)
            {
                l.WaitOne();
            }
            else
            {
                l.WaitOne((int)(promiseTimeoutInSeconds * 1000));   
            }
            c.Stop();
        }
        
        public BasicTaskResultHandle GetResultOfTaskAsync(ITask t)
        {
            return GetResultOfTaskAsync(t.GetIdentifier());
        }

        public BasicTaskResultHandle GetResultOfTaskAsync(string taskInstanceId)
        {
            var key = taskInstanceId;
            var result = new BasicTaskResultHandle(key, this);

            PromiseLikeRedisPubSub(key, (channel, msg) =>
            {
                result.Result = msg;
            });
            
            return result;
        }

        public object GetRawResult(string taskInstanceId)
        {
            throw new NotImplementedException();
        }

        public object GetRawResult(ITask t)
        {
            throw new NotImplementedException();
        }

        public bool SetResultOfTask(ITask t)
        {
            // TODO: somehow make this reliable because pubsub is not reliable.
            // I mean pubsub does not guarentee that the published message has reached a subscriber
            // a possible solution: add this task to a set which holds waitingForAck tasks
            using var redis = _redisClientsManager.GetClient();
            redis.PublishMessage(t.GetInstanceIdentifier(), t.GetObjectResult().ToString());
            return true;
        }

        public void Acknowledge(ITask t)
        {
            using var redis = _redisClientsManager.GetClient();
            //remove this task instance from waitingForAck redis hash
        }
    }
}