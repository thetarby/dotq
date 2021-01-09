using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using dotq.Storage;
using Newtonsoft.Json;
using ServiceStack;
using ServiceStack.Redis;
using ServiceStack.Script;
using StackExchange.Redis;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace test
{
    public class TestRedisPromise
    {
        public static void test1()
        {
            ConnectionMultiplexer redis = ConnectionMultiplexer.Connect("localhost");
            var proClient = new RedisPromiseClient(redis);
            
            var promise = proClient.Listen("haha");
            promise.OnTimeOut = () => { promise.StartRetryThread(); };
            promise.OnResolve = (payload) => { Console.WriteLine($"I AM RESOLVED BABEEEE: {payload}"); };

            if (promise.Payload != null || promise.IsResolved() == true)
                throw new Exception();


            var server = new RedisPromiseServer(redis);
            server.Resolve(new PromiseIdChannelIdDto() {ChannelId = proClient.GetId().ToString(), PromiseId = "haha"},
                "42");
            for (int i = 0; i < 10; i++)
            {
                Thread.Sleep(50);
            }

            if (promise.Payload == null || promise.IsResolved() == false || (string) promise.Payload != "42")
                throw new Exception();

        }
        
        
        public static void testRetry()
        {
            ConnectionMultiplexer redis = ConnectionMultiplexer.Connect("localhost");
            var proClient = new RedisPromiseClient(redis);
            
            var promise = proClient.Listen("haha");
            promise.OnTimeOut = () => { promise.StartRetryThread(); };
            promise.OnResolve = (payload) => { Console.WriteLine($"I AM RESOLVED BABEEEE: {payload}"); };

            if (promise.Payload != null || promise.IsResolved() == true)
                throw new Exception();


            var server = new RedisPromiseServer(redis);
            server.ResolveWithoutPublishing(promise.GetPromiseId().ToString(), "42");

            Thread.Sleep(100);


            if (promise.Payload != null || promise.IsResolved() == true)
                throw new Exception();
    
            promise.Retry();
            
            Thread.Sleep(100);
            
            if (promise.Payload == null || promise.IsResolved() == false || ((string)promise.Payload)!="42")
                throw new Exception();
            
            Console.WriteLine("Retry test is successful. Except that close thread is still running :(");
            // Problem here is that when a promise is resolved with ResolveWithoutPublishing and using retry then client does not know anything about promise resolving hence do not unsubscribe. 
            // Probable Solution is to call clients onmessage from promise by making promise and client more coupled.
            
            
        }
        
        public static void StressTest()
        {
            ConnectionMultiplexer redis = ConnectionMultiplexer.Connect("localhost");
            var proClient = new RedisPromiseClient(redis);
            var clientguid = proClient.GetId().ToString();
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
                server.Resolve(new PromiseIdChannelIdDto(){ChannelId = clientguid, PromiseId = i.ToString()}, i.ToString());
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

                if (promise.Payload == null || promise.IsResolved() == false || (string) promise.Payload != i.ToString())
                    throw new Exception();
            }

            Console.WriteLine("Stress test is successful");
        }

        public static void RedisPubSubBug()
        {
            /*
             * At the beginning I was using servicestack redis client implementation but below is why I changed it to stackexchange client.
             * Basically when you publish burst messages subscriber does not call onmessage handler for each message.(something like buffer overflowing occurs probably)
             * stackexchange client queues messages and does not have this problem. 
             */
            
            var clientsManager = new PooledRedisClientManager();

            var guid = Guid.NewGuid();
            int count = 0;
            var pubsub = new RedisPubSubServer(clientsManager, guid.ToString())
            {
                OnMessage = ((s, s1) =>
                {
                    count++;
                    Thread.Sleep(50);
                }),
            };

            pubsub.Start();

            var anotherClientsManager = new PooledRedisClientManager();
            var client = anotherClientsManager.GetClient();
            for (int i = 0; i < 100; i++)
            {
                client.PublishMessage(guid.ToString(), "asd");
            }

            pubsub.Dispose();

            Console.WriteLine(count);
        }

        public static void RedisPubSubNotBug()
        {
            var PublishCount = 100;

            var clientsManager = new PooledRedisClientManager();
            var native = (IRedisNativeClient) clientsManager.GetClient();
            //native.Subscribe(proClient.GetId().ToString());
            var guid = Guid.NewGuid();
            int count = 0;

            var multiBytes = native.Subscribe(guid.ToString());
            var t = new Thread(o =>
            {
                while (count < PublishCount-1)
                {
                    multiBytes = native.ReceiveMessages();
                    ParseSubscriptionResults(multiBytes, ((s, s1) =>
                    {
                        count++;
                        Thread.Sleep(100);
                        Console.WriteLine($"count: {count} message:{s1}");}));
                }
            });
            t.Start();

            var anotherClientsManager = new PooledRedisClientManager();
            var client = anotherClientsManager.GetClient();
            for (int i = 0; i < PublishCount; i++)
            {
                client.PublishMessage(guid.ToString(), "asd");
            }

            native.UnSubscribe(guid.ToString());

            Console.WriteLine(count);
        }

        static public void ParseSubscriptionResults(byte[][] multiBytes, Action<string, string> OnMessage)
        {
            byte[] MessageWord = "message".ToUtf8Bytes();
            int MsgIndex = 2;
            for (var i = 0; i < multiBytes.Length; i += 3)
            {
                var messageType = multiBytes[i];
                var channel = multiBytes[i + 1].FromUtf8Bytes();
                if (MessageWord.AreEqual(messageType))
                {
                    var msgBytes = multiBytes[i + MsgIndex];

                    var message = msgBytes.FromUtf8Bytes();
                    OnMessage?.Invoke(channel, message);
                }
            }

        }
        
        
        public static void RedisPubSubExchangeConcurrent()
        {
            ConnectionMultiplexer redis = ConnectionMultiplexer.Connect("localhost");
            ISubscriber sub = redis.GetSubscriber();
            int PublishCount = 100;
            object lck= new object();
            
            var guid = Guid.NewGuid();
            int count = 0;
            
            var t = new Thread(o =>
            {
                sub.Subscribe(guid.ToString(), (channel, message) =>
                {
                    lock (lck)
                    {
                        count++;
                        Console.WriteLine($"Count:{count}, msg:{message}");    
                    }
                });
                var x = sub.Subscribe("asd");
                while (count < PublishCount-1)
                {
                    Thread.Sleep(10);
                }
                sub.Unsubscribe(guid.ToString());
            });
            t.Start();
            
            Thread.Sleep(1000); //Give Some Time to establish pubsub client connection
            
            ISubscriber pub = redis.GetSubscriber();
            for (int i = 0; i < PublishCount; i++)
            {
                pub.Publish(guid.ToString(), i.ToString());
            }
            

            Console.WriteLine(count);
        }
        
        public static void RedisPubSubExchangeSequential()
        {
            ConnectionMultiplexer redis = ConnectionMultiplexer.Connect("localhost");
            ISubscriber sub = redis.GetSubscriber();
            int PublishCount = 10000;
            object lck= new object();
            
            var guid = Guid.NewGuid();
            int count = 0;
            
            var t = new Thread(o =>
            {
                var channelSubscription=sub.Subscribe(guid.ToString());
                channelSubscription.OnMessage((message) =>
                {
                    lock (lck)
                    {
                        count++;
                        Console.WriteLine($"Count:{count}, msg:{message}");    
                    }
                });
                var x = sub.Subscribe("asd");
                while (count < PublishCount-1)
                {
                    Thread.Sleep(10);
                }
                channelSubscription.Unsubscribe();
            });
            t.Start();
            
            Thread.Sleep(1000); //Give Some Time to establish pubsub client connection
            
            ISubscriber pub = redis.GetSubscriber();
            for (int i = 0; i < PublishCount; i++)
            {
                pub.Publish(guid.ToString(), i.ToString());
            }
            

            Console.WriteLine(count);
        }
    }
}