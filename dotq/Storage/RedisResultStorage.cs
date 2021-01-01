﻿using System;
using System.Threading;
using dotq.Task;
using dotq.TaskResult;
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

        private void CloseRedisPubSub(object o)
        {
            var pair = ((RedisPubSubServer, Semaphore)) o;
            var l = pair.Item2;
            var c = pair.Item1;
            l.WaitOne();
            c.Stop();
        }
        
        
        public BasicTaskResult GetResultOfTaskAsync(ITask t)
        {
            // lock which prevents CloseRedisPubSub thread to close a connection
            // when onmessage handler is called this lock is relased and the thread that calls CloseRedisPubSub will close the connection
            Semaphore luck=new Semaphore(0,1);
            var result = new BasicTaskResult();
            
            // subscribe to the channel with the name task_id+task_creation_time which is spesific to a task instance
            // creation time might not be unique hence it should be changed
            var redisPubSub = new RedisPubSubServer(_redisClientsManager, t.GetIdentifier()+t.GetCreationTime())
            {
                OnMessage = (channel, msg) =>
                {
                    if (true)
                    {
                        result.Result = msg;
                        luck.Release();
                    }
                    else Console.WriteLine(msg);
                },
            };
            
            var obj=(redisPubSub, luck);
            
            // start thread which will close the connection after acquiring the lock
            Thread thread = new Thread(new ParameterizedThreadStart(CloseRedisPubSub));
            thread.Start(obj);
            redisPubSub.Start();

            return result;
        }
        
        public bool SetResultOfTask(ITask t)
        {
            using var redis = _redisClientsManager.GetClient();
            redis.PublishMessage(t.GetIdentifier()+t.GetCreationTime(), t.GetObjectResult().ToString());
            return true;
        }
    }
}