using System;
using System.Collections.Generic;
using System.Threading;
using dotq.Storage;
using dotq.Storage.RedisPromise;
using StackExchange.Redis;
using Xunit;
using Xunit.Abstractions;

namespace TestProject1
{
    [Collection("Sequential")]
    public class RedisPromiseTest
    {
        private ITestOutputHelper _testOutputHelper;

        public RedisPromiseTest(ITestOutputHelper testOutputHelper)
        {
            _testOutputHelper = testOutputHelper;

        }
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
        
        
        [Fact]
        public void PersistentRedisPromiseClientPoolTest()
        {
            int promiseCount = 20;
            var redis = ConnectionMultiplexer.Connect("localhost");
            var server = new RedisPromiseServer(redis);
            var output = new List<string>();
            var pool = new PersistentRedisPromiseClientPool(redis);
            
            for (int i = 0; i < promiseCount; i++)
            {
                /* when a new instance is created here each time instead of getting from pool
                 this test takes more than a minute for 20 promises. Although base RedisPromiseClient is really
                 cheap to construct it is really expensive to call SetupPubSubServer for the
                 first time since it sets up pubsub channel with redis. Hence creating a new instance
                 of RedisPromiseClient and consume it quickly is a really slow way of using it.
                 Client instances should be taken from the pool when that is the case. (Using the 
                 pool at all times might be a good solution as well since it is not harmful 
                 or does not have performance penalties for other use cases too. Just no performance 
                 gain from pooling.) */
                var proClient = pool.Borrow();
                var promise = proClient.Listen(i.ToString());
                
                promise.OnResolve = (payload) =>
                {
                    //_testOutputHelper.WriteLine("hello");
                    output.Add("hello");
                    pool.Return(proClient);
                    //Assert.Equal((string)payload, i.ToString());
                };
                if (i == 1)
                {
                    var xas = 1;
                }
                
                server.Resolve(promise.GetPromiseId(), i.ToString());
            }
            
            Console.WriteLine("Stress test is successful");
        }
    }
}