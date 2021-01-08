using System;
using System.Threading;
using dotq.Storage;
using ServiceStack.Redis;

namespace test
{
    public class TestRedisPromise
    {
        public static void test1()
        {
            var clientsManager = new PooledRedisClientManager();
            var proClient = new RedisPromiseClient(clientsManager);

            var promise = proClient.Listen("haha");
            promise.OnTimeOut = () =>
            {
                promise.StartRetryThread();
            };
            
            if (promise.Payload != null || promise.IsResolved() == true)
                throw new Exception();
            

            var server = new RedisPromiseServer(clientsManager);
            server.Resolve("haha", "42");
            for (int i = 0; i < 10; i++)
            {
                Thread.Sleep(50);    
            }
            if (promise.Payload == null || promise.IsResolved() == false || (string) promise.Payload!="42")
                throw new Exception();
            
        }
    }
}