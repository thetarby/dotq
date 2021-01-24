using System;
using System.Collections.Generic;
using System.Threading;
using dotq.Storage;
using StackExchange.Redis;
using Xunit;

namespace TestProject1
{
    [Collection("Sequential")]
    public class RedisPromiseTest
    {
        [Fact]
        public static void StressTestWithConcurrentRedisPromiseClient()
        {
            ConnectionMultiplexer redis = ConnectionMultiplexer.Connect("localhost");
            var proClient = new ConcurrentRedisPromiseClient(redis);
            var promises = new List<Promise>();
            int promiseCount = 1000;

            for (int i = 0; i < promiseCount; i++)
            {
                var promise = proClient.Listen(i.ToString());
                promise.OnResolve = (payload) =>
                {
                    //Console.WriteLine($"Promise({promise.GetPromiseId().ToString()}) is resolved. Payload: {payload}");
                };
                if (promise.Payload != null || promise.IsResolved() == true)
                    throw new Exception();
                promises.Add(promise);
            }


            var server = new RedisPromiseServer(redis);
            for (int i = 0; i < promiseCount; i++)
            {
                //server.Resolve(new PromiseIdChannelIdDto(){ChannelId = clientguid, PromiseId = i.ToString()}, i.ToString()); // or;
                server.Resolve(promises[i].GetPromiseId(), i.ToString());
            }

            Thread.Sleep(100);
            for (int i = 0; i < promiseCount; i++)
            {
                var promise = promises[i];
                while (promise.IsResolved()==false)
                {
                    Console.WriteLine("waiting");
                    Thread.Sleep(100);
                }
                
                Assert.NotNull(promise.Payload);
                Assert.True(promise.IsResolved());
                Assert.Equal(i.ToString(), (string) promise.Payload);

            }

            Console.WriteLine("Stress test is successful");
        }
        
        [Fact]
        public static void StressTestWithRedisPromiseClient()
        {
            ConnectionMultiplexer redis = ConnectionMultiplexer.Connect("localhost");
            var proClient = new RedisPromiseClient(redis);
            var promises = new List<Promise>();
            int promiseCount = 1000;

            for (int i = 0; i < promiseCount; i++)
            {
                var promise = proClient.Listen(i.ToString());
                promise.OnResolve = (payload) =>
                {
                    Console.WriteLine($"Promise({promise.GetPromiseId().ToString()}) is resolved. Payload: {payload}");
                };
                if (promise.Payload != null || promise.IsResolved() == true)
                    throw new Exception();
                promises.Add(promise);
            }


            var server = new RedisPromiseServer(redis);
            for (int i = 0; i < promiseCount; i++)
            {
                //server.Resolve(new PromiseIdChannelIdDto(){ChannelId = clientguid, PromiseId = i.ToString()}, i.ToString()); // or;
                server.Resolve(promises[i].GetPromiseId(), i.ToString());
            }

            Thread.Sleep(100);
            for (int i = 0; i < promiseCount; i++)
            {
                var promise = promises[i];
                while (promise.IsResolved()==false)
                {
                    Console.WriteLine("waiting");
                    Thread.Sleep(100);
                }
                
                Assert.NotNull(promise.Payload);
                Assert.True(promise.IsResolved());
                Assert.Equal(i.ToString(), (string) promise.Payload);

            }

            Console.WriteLine("Stress test is successful");
        }
        
    }
}