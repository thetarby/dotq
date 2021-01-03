using System;
using System.Collections.Generic;
using System.Threading;
using ServiceStack.Redis;

/*
 * example usage:
 * Promise p= promiseClient.Listen("someuniquevalue"); // this unique value can be a ITask.GetInstanceIdentifier()
 * p.OnResolve=()=> { Console.Writeline("I am resolved!!!")};
 * p.IsResolved; // false
 * p.Payload == null; // true
 * 
 * then some other process, which might be a different machine;
 *
 * promiseServer.Resolve("someuniquevalue", "42");
 *
 * then in the client promise will be resolved;
 * // I am resolved!!! will be written in console
 * p.IsResolved; // true
 * (string)p.Payload; // "42"
 */

// TODOS: an exponential default retry policy for timeout promises 

namespace dotq.Storage
{
    public static class Constants
    {
        // redis key for the hash which keeps promises
        public static string PendingPromises = "pendingPromises";
    }
    
    public class Promise
    {
        private bool _isTimedOut=false;
        private readonly string _resultChannel; // result channel must be unique far all active promises
        private readonly IRedisClient _client;
        
        public object Payload { get; set; }
        
        
        public Action OnResolve { get; set; } = null;
        
        
        public Action OnTimeOut { get; set; } = null;
        
        
        public void _OnResolveBase()
        {
            OnResolve?.Invoke();
            var pendingPromisesSet = _client.Hashes[Constants.PendingPromises];
            pendingPromisesSet.Remove(_resultChannel);
            
            _client.Dispose();
        }
        
        
        public void _OnTimeOutBase()
        {
            OnTimeOut?.Invoke();
        }
        
        
        public Promise(IRedisClient client, string resultChannel)
        {
            Payload = null;
            _resultChannel = resultChannel;
            _client = client;
        }
        
        
        public bool IsResolved() => Payload != null;
        
        
        public bool IsTimedOut() => _isTimedOut;
        
        
        // this is called by PromiseClient internally if Promise timeouts
        public void TimedOut()
        {
            _isTimedOut = true;
            _OnTimeOutBase();
        }
        
        
        // forcefully tries to resolve the promise.
        // (It assumes that pubsub somehow failed to publish message and looks for the result in pendingPromises hash.)
        // It returns true if it succeeds to resolve else returns false
        public bool Retry()
        {
            var pendingPromises = _client.Hashes[Constants.PendingPromises];
            
            // for listen2-resolve2 this should always return true hence retry should change when using them
            if (pendingPromises.ContainsKey(_resultChannel))
            {
                var res=pendingPromises[_resultChannel];
                Payload = res;
                _OnResolveBase();
                return true;
            }

            return false;
        }
    }
    
    
    public class RedisPromiseClient
    {
        private IRedisClientsManager _redisClientsManager;

        public RedisPromiseClient(IRedisClientsManager redisClientsManager)
        {
            _redisClientsManager = redisClientsManager;
        }

        
        private Thread CloseConnectionThread(
            RedisPubSubServer redisPubSubServer, 
            Semaphore luck, 
            float timeoutInSeconds=-1, 
            Promise promise=null
            )
        {
            if (promise == null && timeoutInSeconds != -1)
                throw new Exception("if timeout is specified a promise should be passed");
            
            Thread thread = new Thread(new ParameterizedThreadStart(CloseConnectionThread));
            thread.Start((object)(redisPubSubServer,luck, timeoutInSeconds, promise));
            return thread;
        }
        
        private void CloseConnectionThread(object o)
        {
            var args = ((RedisPubSubServer, Semaphore, float, Promise)) o;
            var promiseTimeoutInSeconds = args.Item3;
            var promise = args.Item4;
            
            var l = args.Item2;
            var c = args.Item1;
            
            if (promiseTimeoutInSeconds == -1)
            {
                l.WaitOne();
            }
            else
            {
                l.WaitOne((int)(promiseTimeoutInSeconds * 1000));
                promise.TimedOut();
            }
            c.Stop();
            c.Dispose();
        }
        
        
        /*
         * listens for a resultChannel, which is a redis channel which will publish the result of an action(or task)
         * Immediately returns a promise which will later be resolved with the published message.
         */
        public Promise Listen(string resultChannel)
        {
            var client = _redisClientsManager.GetClient();
            var resultPromise = new Promise(client, resultChannel);
            
            // lock which prevents CloseRedisPubSub thread to close a connection
            // when onmessage handler is called this lock is released and the thread that calls
            // CloseRedisPubSub will close the connection
            Semaphore luck = new Semaphore(0,1);
            
            // subscribe to the channel with the name task_id+task_creation_time which is spesific to a task instance
            // creation time might not be unique hence it should be changed
            var redisPubSub = new RedisPubSubServer(_redisClientsManager, resultChannel)
            {
                OnMessage = (channel, msg) =>
                {
                    resultPromise.Payload = msg;
                    resultPromise._OnResolveBase();
                    luck.Release();
                },
            };
        
            CloseConnectionThread(redisPubSub, luck); // this starts thread
            redisPubSub.Start();

            return resultPromise;
        }


        /*
         * a different implementation;
         * 1 first adds a promise with empty message to pending promises.
         * 2 server populates message
         * 3 entry is removed from pending promises on resolve
         * NOTE: this is not working right now. Server counterparts should be implemented as well.
         */
        public Promise Listen2(string resultChannel)
        {
            var client = _redisClientsManager.GetClient();
            var pendingPromises = client.Hashes[Constants.PendingPromises];
            var success= pendingPromises.AddIfNotExists(new KeyValuePair<string, string>(resultChannel, ""));

            if (success == false)
                throw new Exception("Failed while trying to create a promise. " +
                                    "Probably there was another promise with the same result channel");
                    
            var resultPromise = new Promise(client, resultChannel);
            Semaphore luck = new Semaphore(0,1);
            var redisPubSub = new RedisPubSubServer(_redisClientsManager, resultChannel)
            {
                OnMessage = (channel, msg) =>
                {
                    resultPromise.Payload = msg;
                    resultPromise._OnResolveBase();
                    luck.Release();
                },
            };
        
            CloseConnectionThread(redisPubSub, luck).Start();
            redisPubSub.Start();

            return resultPromise;
        }
    }

    
    public class RedisPromiseServer
    {
        private IRedisClientsManager _redisClientsManager;

        public RedisPromiseServer(IRedisClientsManager redisClientsManager)
        {
            _redisClientsManager = redisClientsManager;
        }

        public void Resolve(string channel, string message)
        {
            using var client = _redisClientsManager.GetClient();
            
            // put message to a hash also in case consumer cannot receive the published message(due to timeout maybe?)
            // in that case client can retry to get the message from pendingPromises
            client.Hashes[Constants.PendingPromises].Add(channel, message);
            client.PublishMessage(channel, message);
        }
        
        public void Resolve2(string channel, string message)
        {
            using var client = _redisClientsManager.GetClient();
            
            // put message to a hash also in case consumer cannot receive the published message(due to timeout maybe?)
            // in that case client can retry to get the message from pendingPromises
            client.Hashes[Constants.PendingPromises][channel] = message;
            client.PublishMessage(channel, message);
        }
    }
}