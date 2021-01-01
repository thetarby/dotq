using System;
using System.Threading;
using ServiceStack.Redis;

namespace test
{
    public class Pubsub
    {
        private static void CloseRedis(object o)
        {
            var x = ((RedisPubSubServer, Semaphore)) o;
            var l = x.Item2;
            var c = x.Item1;
            l.WaitOne(); 
            c.Stop();
        }
        
        static void NotMain(string[] args)
        {
            Console.WriteLine("Hello World!");
            var clientsManager = new PooledRedisClientManager();
            Semaphore luck=new Semaphore(0,1);
            var redisPubSub = new RedisPubSubServer(clientsManager, "addtask")
            {
                OnMessage = (channel, msg) =>
                {
                    if (msg == "aranan") luck.Release();
                    else Console.WriteLine(msg);
                },
            };
            var obj=(redisPubSub, luck);
            Thread t = new Thread(new ParameterizedThreadStart(CloseRedis));
            t.Start(obj);
            redisPubSub.Start();
            Console.WriteLine("redis started");
            
            while (true)
            {
                Thread.Sleep(100);
            }
            
            
            Console.WriteLine("finished");
        }
    }
}