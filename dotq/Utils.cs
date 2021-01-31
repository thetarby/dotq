using System;
using StackExchange.Redis;

namespace dotq
{
    public class Utils
    {
        public static class LocalRedis
        {
            private static readonly Lazy<ConnectionMultiplexer> _lazy = new(() =>  ConnectionMultiplexer.Connect("localhost"));
        
            public static ConnectionMultiplexer Instance => _lazy.Value;
        }
    }
}