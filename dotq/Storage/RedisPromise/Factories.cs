using System;
using System.Collections.Generic;
using System.Linq;
using dotq.Storage.Pooling;
using StackExchange.Redis;

namespace dotq.Storage.RedisPromise
{
    /* NOTE: when a new instance is created here each time instead of getting from pool
    this test takes more than a minute for 20 promises. Although base RedisPromiseClient is really
    cheap to construct it is really expensive to call SetupPubSubServer for the
    first time since it sets up pubsub channel with redis. Hence creating a new instance
    of RedisPromiseClient and consume it quickly is a really slow way of using it.
    Client instances should be taken from the pool when that is the case. (Using the 
    pool at all times might be a good solution as well since it is not harmful 
    or does not have performance penalties for other use cases too. Just no performance 
    gain from pooling.) */
    
    /// <summary>
    /// Should be used to get PersistentRedisPromiseClient instances. PersistentRedisPromiseClient does not close
    /// its connection so it is useful in cases where Client instances are created and consumed very quickly. It prevents
    /// to create new connections(subscriptions in this case) to redis.
    ///
    /// NOTE
    /// </summary>
    public class PersistentRedisPromiseClientPool : GenericPool<PersistentRedisPromiseClient>
    {
        private ConnectionMultiplexer _redis;

        public PersistentRedisPromiseClientPool(ConnectionMultiplexer redis=null, TimeSpan? deadTime=null) : base(deadTime)
        {
            _redis = redis ?? ConnectionMultiplexer.Connect("localhost");
        }

        protected override PersistentRedisPromiseClient Create()
        {
            return new PersistentRedisPromiseClient(_redis);
        }

        protected override void DisposeResource(PersistentRedisPromiseClient resource)
        {
            resource.Dispose();
        }
    }
    
    
    /// <summary>
    /// A factory-like class which returns same instance of promiseClient as long as passed redis instance is the same.
    /// Should be used to get base RedisPromiseClient and ConcurrentRedisPromiseClient instances
    /// </summary>
    public static class RedisPromiseClientFactory
    {
        private static readonly object lck = new object();  
        private static List<RedisPromiseClient> _instances = new List<RedisPromiseClient>();  
        public static RedisPromiseClient GetInstance(ConnectionMultiplexer redis)  
        {
            lock (lck)  
            {
                if (_instances.Exists((client => client.GetRedisInstance() == redis)))
                {
                    return _instances.First(d => d.GetRedisInstance()==redis);
                }
                
                var client = new RedisPromiseClient(redis);
                _instances.Add(client);
                return client;  
            }
        }
    }
}