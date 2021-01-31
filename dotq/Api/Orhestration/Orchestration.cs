using System;
using dotq.Storage.RedisPromise;
using StackExchange.Redis;

namespace dotq.Api.Orhestration
{
    /// <summary>
    /// singleton providing access to redis
    /// </summary>
    public static class LocalRedis
    {
        private static readonly Lazy<ConnectionMultiplexer> _lazy = new(() =>  ConnectionMultiplexer.Connect("localhost"));
        
        public static ConnectionMultiplexer Instance => _lazy.Value;
    }

    
    public static class PromiseClientProvider
    {
        public static RedisPromiseClient GetInstance(ConnectionMultiplexer redis)
        {
            return RedisPromiseClientFactory.GetInstance(redis);
        }
    }
    
    
    public static class PromiseServerProvider
    {
        public static RedisPromiseServer GetInstance()
        {
            return new RedisPromiseServer();
        }
    }
}