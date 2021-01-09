using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Runtime.InteropServices.ComTypes;
using System.Text.Json;
using System.Threading;
using ServiceStack.Redis;
using StackExchange.Redis;

/*
 * example usage:
 * Promise p= promiseClient.Listen("someuniquevalue"); // this unique value can be a ITask.GetInstanceIdentifier()
 * p.OnResolve=()=> { Console.Writeline("I am resolved!!!")};
 * p.IsResolved; // false
 * p.Payload == null; // true
 * 
 * then some other process, which might be a different machine;
 *
 * promiseServer.Resolve(promise.GetPromiseId(), "42"); // since resolve server will be a different machine it will not have access to promise instance so promise id should come from somewhere else in real life.
 *                                                      // (from a request or from pending promises hash in redis) like;
 *                                                      // promiseServer.Resolve(promiseServer.GetRedisInstance().GetDatabase().HashGetAll(Constants.PendingPromises)[0].Value.ToString(), "42")
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
    
    
    // Simple retry logic which can exponentially increase sleeping time after each retry. Usage;
    // SimpleRetry.ExponentialDo(() =>{ return DoSomethingThatCanThrowException() }, TimeSpan.FromSeconds(0.1));
    // wait times will be: 0.1 => 0.2 => 0.4 => 0.8
    public static class SimpleRetry
    {
        public static void Do(
            Action action,
            TimeSpan retryInterval,
            int maxAttemptCount = 3)
        {
            Do<object>(() =>
            {
                action();
                return null;
            }, retryInterval, maxAttemptCount);
        }

        public static void ExponentialDo(
            Action action,
            TimeSpan firstRetrySpan,
            int maxAttemptCount = 5)
        {
            for (int i = 0; i < maxAttemptCount; i++)
            {
                Do<object>(() =>
                {
                    action();
                    return null;
                }, Math.Pow(2, i) * firstRetrySpan, 1);   
            }
        }

        public static T Do<T>(
            Func<T> action,
            TimeSpan retryInterval,
            int maxAttemptCount = 3)
        {
            var exceptions = new List<Exception>();

            for (int attempted = 0; attempted < maxAttemptCount; attempted++)
            {
                try
                {
                    if (attempted == 0) // in first attempt dont sleep
                        return action();
                    else
                        Thread.Sleep(retryInterval);
                }
                catch (Exception ex)
                {
                    exceptions.Add(ex);
                }
            }
            throw new AggregateException(exceptions);
        }
    }
    
    
    public class Promise
    {
        private bool _isTimedOut=false;
        private readonly string _id; // result channel must be unique far all active promises
        private readonly ConnectionMultiplexer _redis;
        private readonly RedisPromiseClient _promiseClient;
        
        public object Payload { get; set; }
        
        
        public Action<object> OnResolve { get; set; } = null;
        
        
        public Action OnTimeOut { get; set; } = null;
        
        
        public void _OnResolveBase()
        {
            OnResolve?.Invoke(Payload);
            var db = _redis.GetDatabase();
            var success = db.HashDelete(Constants.PendingPromises,GetPromiseId().ToString());
            if (success == false)
                throw new Exception("Resolved an already resolved promise. Duplicate promise ids maybe?");
            
            //_client.Dispose();
        }
        
        
        public void _OnTimeOutBase()
        {
            OnTimeOut?.Invoke();
        }
        
        
        public Promise(ConnectionMultiplexer redis, string id)
        {
            Payload = null;
            _id = id;
            _redis = redis;
        }
        
        
        // creates a promise instance which is connected to a promise client
        public Promise(RedisPromiseClient promiseClient, string id)
        {
            Payload = null;
            _id = id;
            _promiseClient = promiseClient;
            _redis = promiseClient.GetRedisInstance();
        }
        
        
        public bool IsResolved() => Payload != null;
        
        
        public bool IsTimedOut() => _isTimedOut;
        
        
        public bool IsConnected() => _promiseClient!=null;


        public string GetPromiseId()
        {
            // id of a promise is prefixed with related promise client id to be able to find the correct channel
            if (IsConnected())
            {
                return _promiseClient.GetId().ToString() + ':' + _id;
            }

            return _id;
        }
        
        
        // this is called by PromiseClient internally if Promise timeouts
        public void TimedOut()
        {
            _isTimedOut = true;
            _OnTimeOutBase();
        }
        
        
        // forcefully tries to resolve the promise by looking at the list of promises instead of waiting pubsub notification.
        // (It assumes that pubsub somehow failed to publish message and looks for the result in pendingPromises hash.)
        // It returns true if it succeeds to resolve else returns false
        public bool Retry()
        {
            var db = _redis.GetDatabase();
            var pendingPromises = db.HashGetAll(Constants.PendingPromises).ToDictionary();
            var key = GetPromiseId();
            
            // for listen2-resolve2 this should always return true hence retry should change when using them
            if (pendingPromises.ContainsKey(key))
            {
                var res=pendingPromises[key].ToString();
                Payload = res;
                _OnResolveBase();
                return true;
            }

            throw new Exception("Cannot resolve. Probably a server did not resolve the promise yet");
            //return false;
        }

        
        // starts a background thread that call retry with exponentially increasing waiting in between
        public Thread StartRetryThread()
        {
            Thread t=new Thread((o =>
            {
                // int startingWaitingTime = (int) o;
                SimpleRetry.ExponentialDo(() =>
                {
                    Retry();
                }, TimeSpan.FromSeconds(0.1));
            }));
            
            t.Start();
            return t;
        }
    }
    
    
    public class RedisPromiseClient
    {
        private ConnectionMultiplexer _redis;
        private ChannelMessageQueue _channelSubscription;
        private Guid _guid;
        private Dictionary<string, Promise> _mapper;
        private int countTime=0;
        private object mapLock = new object();

        public RedisPromiseClient(ConnectionMultiplexer  redis)
        {
            _redis = redis;
            _channelSubscription = null;
            _guid = Guid.NewGuid();
            _mapper = new Dictionary<string, Promise>();
        }


        public ConnectionMultiplexer GetRedisInstance() => _redis;
        
        public Guid GetId() => _guid;
        
        
        private Thread CloseConnectionThread(
            ChannelMessageQueue channelSubscription, 
            Semaphore luck, 
            float timeoutInSeconds=-1, 
            Promise promise=null
            )
        {
            if (promise == null && timeoutInSeconds != -1)
                throw new Exception("if timeout is specified a promise should be passed");
            
            Thread thread = new Thread(new ParameterizedThreadStart(CloseConnectionThread));
            thread.Start((object)(channelSubscription,luck, timeoutInSeconds, promise));
            return thread;
        }
        
        
        private void CloseConnectionThread(object o)
        {
            var args = ((ChannelMessageQueue, Semaphore, float, Promise)) o;
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
            
            Console.WriteLine("ALL PROMISES RESOLVED UNSUBSCRIBING...");
            c.Unsubscribe();
        }
        
        
        /*
         * listens for a resultChannel, which is a redis channel which will publish the result of an action(or task)
         * Immediately returns a promise which will later be resolved with the published message.
         */
        public Promise Listen(string resultChannel)
        {
            var promiseId = resultChannel;
            
            // create a connected promise
            var resultPromise = new Promise(this, promiseId);
            
            _mapper.Add(promiseId, resultPromise);
            
            var redisPubSub = GetPubSubServer();

            return resultPromise;
        }

        
        //sequential
        // sets up a pubsub subscription for this instance. When called many times returns same subscription like a singleton. But unlike singleton it can dispose subscription after no one is using it
        // hence it should be called before creating any promise otherwise there may not be any thread listening for promise resolving messages.
        ChannelMessageQueue GetPubSubServer() 
        {
            lock (this)
            {
                if (this._channelSubscription == null)
                {
                    Semaphore luck = new Semaphore(0,1);

                    var channelSubscription = _redis.GetSubscriber().Subscribe(_guid.ToString());
                    channelSubscription.OnMessage((message =>
                    {
                        countTime++;
                        lock (mapLock)
                        {
                            var promiseIdActualMessageDto = JsonSerializer.Deserialize<PromiseIdActualMessageDto>(message.Message.ToString());
                            var promiseId = promiseIdActualMessageDto.PromiseId;
                            var realmsg = promiseIdActualMessageDto.ActualMessage;
                            
                            if (_mapper.ContainsKey(promiseId))
                            {
                                var promise = _mapper[promiseId];
                                _mapper.Remove(promiseId);
                                promise.Payload = realmsg;
                                promise._OnResolveBase();
                                if (_mapper.Count == 0)
                                {
                                    luck.Release();
                                } 
                            }
                        }
                    }));
                    
                    CloseConnectionThread(channelSubscription, luck); // this starts thread
                    Thread.Sleep(1000); //give some time to establish subscription
                    
                    _channelSubscription = channelSubscription;
                    return channelSubscription;
                }
                
                return _channelSubscription;   
            }
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
            
            /*var client = _redisClientsManager.GetClient();
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
                OnError = (exception => Console.WriteLine($"EXCEPTION....................... {exception}"))
            };
        
            CloseConnectionThread(redisPubSub, luck).Start();
            redisPubSub.Start();

            return resultPromise;*/
            throw  new NotImplementedException();
        }
    }

    
    public class RedisPromiseServer
    {
        private ConnectionMultiplexer _redis;

        public RedisPromiseServer(ConnectionMultiplexer redis)
        {
            _redis = redis;
        }

        ConnectionMultiplexer GetRedisInstance() => _redis;
        
        public void Resolve(string promiseId, string message)
        {
            var x = new int[] {1, 2, 3};
            try
            {
                var split = promiseId.Split(':');
                Resolve(new PromiseIdChannelIdDto()
                {
                    ChannelId = split[0],
                    PromiseId = split[1]
                }, message);
            }
            catch (IndexOutOfRangeException e)
            {
                Console.WriteLine("Error while parsing promiseId. Probably promise was not a connected one. ");
                throw;
            }
        }
        
        
        public void Resolve(PromiseIdChannelIdDto promiseIdChannelIdDto, string message)
        {
            var client = _redis.GetSubscriber();
            
            var promiseId = promiseIdChannelIdDto.PromiseId;
            var channelId = promiseIdChannelIdDto.ChannelId;
            
            // put message to a hash also in case consumer cannot receive the published message(due to timeout maybe?)
            // in that case client can retry to get the message from pendingPromises
            IDatabase db = _redis.GetDatabase();
            db.HashSet(Constants.PendingPromises, new[] {new HashEntry(promiseId, message)});
            
            var res = new PromiseIdActualMessageDto()
            {
                PromiseId = promiseId,
                ActualMessage = message
            };
            
            client.Publish(channelId, JsonSerializer.Serialize(res));
        }

        
        public void ResolveWithoutPublishing(string promiseId, string message)
        {
            IDatabase db = _redis.GetDatabase();
            db.HashSet(Constants.PendingPromises, new[] {new HashEntry(promiseId, message)});
        }
        
        
        public void Resolve2(string channel, string message)
        {
            // using var client = _redis.GetClient();
            //
            // // put message to a hash also in case consumer cannot receive the published message(due to timeout maybe?)
            // // in that case client can retry to get the message from pendingPromises
            // client.Hashes[Constants.PendingPromises][channel] = message;
            // client.PublishMessage(channel, message);

            throw new NotImplementedException();
        }

    }
    
    public class PromiseIdChannelIdDto
    {
        public string PromiseId { get; set; }
            
        public string ChannelId { get; set; }
    }
    public class PromiseIdActualMessageDto
    {
        public string PromiseId { get; set; }
            
        public string ActualMessage { get; set; }
    }
}