using System;
using System.Threading;
using dotq.Api;
using dotq.Storage;
using dotq.Storage.RedisPromise;
using StackExchange.Redis;
using test.Tasks;

namespace test
{
    public class TestApi
    {
        public static void Test1()
        {
            var api = new DotqApi();
            var mult1=new MultiplyTask(new Inp{x=4,y=5});
            var mult2=new MultiplyTask(new Inp{x=4,y=6});

            var res1=api.Delay(mult1);
            var res2=api.Delay(mult2);

            ConnectionMultiplexer redis = ConnectionMultiplexer.Connect("localhost");
            var m=new RedisTaskQueue(redis);
            var resolver = new RedisPromiseServer();
            
            // Thread.Sleep(100);
            // for (long i = 0; i < m.Length(); i++)
            // {
            //     var task = m.Dequeue();
            //     task.Execute();
            //     resolver.Resolve(task.Promise, task.SerializeResult());
            // }
            // Thread.Sleep(100);
            // if (res1.GetResult() == 20 && res2.GetResult() == 24)
            // {
            //     Console.WriteLine("test is successful");
            // }
        }
    }
}