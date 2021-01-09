using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Net.Http;
using System.Threading;
using dotq.Client;
using dotq.Storage;
using dotq.Task;
using dotq.TaskRegistry;
using ServiceStack.Redis;
using test.Tasks;

namespace test
{
   
    class Program
    {
        static void TestClient()
        {
            var h = new LongPollingClient();
            var t=h.Get("http://slowwly.robertomurray.co.uk/delay/1500/url/https://jsonplaceholder.typicode.com/todos/1");
            for (int i = 0; i < 4; i++)
            {
                Console.WriteLine(t.Status);
                Thread.Sleep(500);
            }
            Console.WriteLine(t.Result);
            var t2 = h.GetAsString("http://slowwly.robertomurray.co.uk/delay/1500/url/https://jsonplaceholder.typicode.com/todos/1");
            for (int i = 0; i < 4; i++)
            {
                Console.WriteLine(t2.Status);
                Thread.Sleep(500);
            }
            Console.WriteLine(t2.Result);
        }

        static void TestGeneral()
        {
            Console.WriteLine("Hello World!");
            var m=new MemoryStorage();
            var mult=new MultiplyTask(new Inp{x=4,y=5});
            var mult2=new MultiplyTask(new Inp{x=4,y=56});
            var add=new AddTask(new (5,6));
            var swap=new SwapTask(new ("abi","naber"));
            var concat=new ConcatTask(new Inp2{x=new List<string>{"f","u","r","k","a","n"}});
            
            //TestClient();
            
            m.Enqueue(mult.Serialize());
            m.Enqueue(mult2.Serialize());
            m.Enqueue(add.Serialize());
            m.Enqueue(swap.Serialize());
            m.Enqueue(concat.Serialize());
            
            // normally these will happen in workers. workers will execute tasks and store their results in 
            // result store. Client will have an TaskResult handle which will update its content when result is ready.
            // below res1 will be all client have. This program.cs is only for testing purposes normally workers and
            // client will be different processes.
            
            
            /* Normally it will look like this;
             *
             * For Client:
             * 
             * TaskResult mult=new MultiplyTask(new Inp{x=4,y=5}); this will enqueue task, will get the handle from store(or somewhere else)
             * TaskResult.result == null => will be true until result is executed by some worker
             *
             * For workers: each worker will dequeue from TaskStore and push results to ResultStore like;
             *
             * ITask task = queue.Dequeue();
             * task.Execute()
             * resultStore.SetResultOfTask(task);
             */
            
            var resultStore = new RedisResultStorage(new PooledRedisClientManager());
            var res1 = resultStore.GetResultOfTaskAsync(mult);
            
            Console.WriteLine("tasks queued");

            
            Console.WriteLine("task will be dequed. Now cleaning task registry to simulate a server...");

            var registry = (ITaskRegistry) TaskRegistry.Instance;
            registry.Clear();
            registry.DiscoverTasks();

            for (int i = m.Count(); i > 0; i--)
            {
                string serialized = (string)m.Dequeue();
                Console.WriteLine($"dequed task: {serialized}");
            
                var task=new DefaultTaskDeserializer().Deserialize(serialized);
                Console.WriteLine("task is executing...");
                task.Execute();
                Console.WriteLine($"time elapsed: {task.GetTimeElapsed().Value.TotalMilliseconds} ms");
                resultStore.SetResultOfTask(task);
            }

            var x = "x";
        }
        static void Main(string[] args)
        {
            TestRedisPromise.StressTest();
            TestRedisPromise.testRetry();
        }
    }
}