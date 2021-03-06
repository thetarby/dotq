﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using dotq.Storage.Pooling;
using dotq.Utils;
using Newtonsoft.Json;
using ServiceStack.Messaging;
using StackExchange.Redis;
using JsonSerializer = System.Text.Json.JsonSerializer;

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

// TODOS: * Make sure promise ids are unique in the process (with a static class variable maybe?)
//        * Singleton promiseClient

namespace dotq.Storage.RedisPromise
{
    public static class Constants
    {
        // redis key for the hash which keeps promises
        public static string PendingPromises = "pendingPromises";
    }
    
    
    /// <summary>
    /// Each promise has an internal id, which is _id and a public id accessed via GetPromiseId.
    /// Public id is basically internal prefixed by promiseClient's id with a ':' sign in between.
    /// This enables resolver to know which client is listening for the promise and which task result handle is
    /// waiting for the promise by only parsing its id.
    /// </summary>
    public class Promise
    {
        private bool _isTimedOut=false;
        private readonly string _id;
        private readonly ConnectionMultiplexer _redis;
        private readonly RedisPromiseClient _promiseClient;
        private readonly SemaphoreSlim _lock = new SemaphoreSlim(0);

        public object Payload { get; set; }
        

        public Action<object> OnResolve { get; set; } = null;
        
        
        public Action OnTimeOut { get; set; } = null;
        
        
        internal void _OnResolveBase()
        {
            OnResolve?.Invoke(Payload);
            var db = _redis.GetDatabase();
            var success = db.HashDelete(Constants.PendingPromises,GetCompositeKey().ToString());
            _lock.Release();

            // TODO: this exception somehow goes silent and prevents background thread to close connection.
            // also for now this is not necessary
            //if (success == false)
            //    throw new Exception("Resolved an already resolved promise. Duplicate promise ids maybe?");
        }
        
        
        // called by retry. Retry passes db instance so that it is not created twice.
        internal void _OnResolveBase(IDatabase redisDb) 
        {
            OnResolve?.Invoke(Payload);
            var success = redisDb.HashDelete(Constants.PendingPromises,GetCompositeKey().ToString());
            //if (success == false)
            //    throw new Exception("Resolved an already resolved promise. Duplicate promise ids maybe?");
        }
        
        
        internal void _OnTimeOutBase()
        {
            OnTimeOut?.Invoke();
        }
        
        
        public Promise(ConnectionMultiplexer redis, string id=null)
        {
            Payload = null;
            _id = id ?? Guid.NewGuid().ToString();
            _redis = redis;
        }
        
        
        // creates a promise instance which is connected to a promise client
        public Promise(RedisPromiseClient promiseClient, string id=null)
        {
            Payload = null;
            _id = id ?? Guid.NewGuid().ToString();
            _promiseClient = promiseClient;
            _redis = promiseClient.GetRedisInstance();
        }


        public object Wait()
        {
            _lock.Wait(); // wait until OnResolve is called and released lock
            return Payload;
        }
        
        
        public bool IsResolved() => Payload != null;
        
        
        public bool IsTimedOut() => _isTimedOut;
        
        
        /// <summary>
        /// Returns true if promise is connected to a promise client. If it is connected it does not mean that
        /// it is active and listening to be resolved. To check if promise is waiting to be resolved(registered in
        /// clients pubsub waiting for a resolver to resolve) call IsListening 
        /// </summary>
        public bool IsConnected() => _promiseClient!=null;


        /// <summary>
        /// Returns true if promise is connected to a promise client and it is registered in pubsub which means it is
        /// waiting to be resolved.
        /// </summary>
        /// <returns></returns>
        public bool IsListening()
        {
            if (IsConnected())
            {
                _promiseClient.IsPromiseInMapper(this);
            }

            return false;
        }


        /// <summary>
        /// returns a combination of promise key and promise client key if promise is connected to a promise client 
        /// </summary>
        public string GetCompositeKey()
        {
            // id of a promise is prefixed with related promise client id to be able to find the correct channel
            if (IsConnected())
            {
                return _promiseClient.GetId().ToString() + ':' + _id;
            }

            return _id;
        }

        
        public string GetPromiseKey() => _id; // returns _id which is id of the promise without connected promiseClients' id part.
        

        public string GetChannelId() => _promiseClient.GetId().ToString();
        
        
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
            var key = GetCompositeKey();
            
            // for listen2-resolve2 this should always return true hence retry should change when using them
            if (pendingPromises.ContainsKey(key))
            {
                var res=pendingPromises[key].ToString();
                Payload = res;
                _OnResolveBase(db);
                return true;
            }

            throw new Exception("Cannot resolve. Probably a server did not resolve the promise yet");
            //return false;
        }

        
        // starts a background thread that calls retry with exponentially increasing waiting in between
        public Thread StartRetryThread()
        {
            Thread t=new Thread(o =>
            {
                // int startingWaitingTime = (int) o;
                SimpleRetry.ExponentialDo(() =>
                {
                    Retry();
                }, TimeSpan.FromSeconds(0.1));
            });
            
            t.Start();
            return t;
        }
    }
    
    
    public class RedisPromiseClient
    {
        protected ConnectionMultiplexer _redis;
        protected ChannelMessageQueue _channelSubscription;
        protected Guid _guid;
        protected Dictionary<string, Promise> _mapper;
        protected int countTime=0;
        protected object mapLock = new object();
        protected Semaphore canContinue = new Semaphore(0, 1);
        protected Semaphore canClose = new Semaphore(0, 1); // this lock is to prevent closeConnectionThread when there is promise in mapper. If there is no promise in mapper this lock is released and connection is closed 
        
        public RedisPromiseClient(ConnectionMultiplexer  redis)
        {
            _redis = redis;
            _channelSubscription = null;
            _guid = Guid.NewGuid();
            _mapper = new Dictionary<string, Promise>();
        }


        public ConnectionMultiplexer GetRedisInstance() => _redis;
        
        
        public Guid GetId() => _guid;
        
        
        // checks if given promise is in mapper of this client. Meaning it is listening to a pubsub channel can ready to be resolved by a promiseResolver server.
        public bool IsPromiseInMapper(Promise p) => _mapper.ContainsKey(p.GetPromiseKey());


        private void CloseConnectionThread(ChannelMessageQueue channelSubscription,
            Semaphore luck,
            float timeoutInSeconds = -1,
            Promise promise = null)
        {
            if (promise == null && timeoutInSeconds != -1)
                throw new Exception("if timeout is specified a promise should be passed");
            
            Thread thread = new Thread(new ParameterizedThreadStart(CloseConnectionThread));
            thread.Start((object)(channelSubscription,luck, timeoutInSeconds, promise));
        }
        
        
        protected virtual void CloseConnectionThread(object o)
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
                // TODO: this does not work now. It was working for per promise connections
                l.WaitOne((int)(promiseTimeoutInSeconds * 1000));
                promise.TimedOut();
            }
            
            Console.WriteLine("ALL PROMISES RESOLVED UNSUBSCRIBING...");
            c.Unsubscribe();
            _channelSubscription = null;
            canContinue.Release();
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

            lock (mapLock)
            {
                _mapper.Add(promiseId, resultPromise);   
            }

            SetupPubSubServer();

            return resultPromise;
        }

        
        public Promise CreatePromise() => new Promise(this, Guid.NewGuid().ToString());


        public void Listen(Promise promise)
        {
            if (promise.IsListening())
            {
                throw new PromiseIsAlreadyListeningOnAnotherClientException();
            }

            if (promise.IsResolved())
            {
                throw new PromiseIsAlreadyResolvedException();
            }

            lock (mapLock)
            {
                if (_mapper.ContainsKey(promise.GetPromiseKey()))
                {
                    throw new PromiseIsAlreadyInMapperException();
                }
                _mapper.Add(promise.GetPromiseKey(), promise);   
            }
            SetupPubSubServer();
        }
        
        
        //sequential
        // sets up a pubsub subscription for this instance. When called many times returns same subscription like a singleton. But unlike singleton it can dispose subscription after no one is using it
        // hence it should be called before creating any promise otherwise there may not be any thread listening for promise resolving messages.
        protected virtual void SetupPubSubServer() 
        {
            lock (mapLock)
            {
                if (this._channelSubscription == null)
                {
                    var channelSubscription = _redis.GetSubscriber().Subscribe(_guid.ToString());
                    channelSubscription.OnMessage((message) => OnMessageHandler(message.Message));
                    
                    CloseConnectionThread(channelSubscription, canClose); // this starts thread
                    Thread.Sleep(1000); //give some time to establish subscription
                    
                    _channelSubscription = channelSubscription;
                    return;
                }
            }
        }


        protected virtual void OnMessageHandler(RedisValue message)
        {
            countTime++;
            lock (mapLock)
            {
                var promiseIdActualMessageDto =
                    JsonSerializer.Deserialize<PromiseIdActualMessageDto>(message.ToString());
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
                        canClose.Release();
                        // to prevent another thread which calls GetPubSubServer to start a subscription while we are closing it, wait until closing thread cleans subscription before releasing mapLock
                        canContinue.WaitOne();
                    } 
                }
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

        public RedisPromiseServer(ConnectionMultiplexer redis=null)
        {
            _redis = redis ?? ConnectionMultiplexer.Connect("localhost");
        }

        ConnectionMultiplexer GetRedisInstance() => _redis;
        
        public void Resolve(string promiseId, string message)
        {
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