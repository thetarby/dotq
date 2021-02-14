using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Net.Http;
using System.Threading;
using dotq.Api;
using dotq.Client;
using dotq.Storage;
using dotq.Task;
using dotq.TaskRegistry;
using ServiceStack.Redis;
using StackExchange.Redis;
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

        static void TestTaskExecuting()
        {
            Console.WriteLine("Hello World!");
            var m=new MemoryQueue();
            var mult=new MultiplyTask(new Inp{x=4,y=5});
            var mult2=new MultiplyTask(new Inp{x=4,y=56});
            var add=new AddTask(new (5,6));
            var swap=new SwapTask(new ("abi","naber"));
            var concat=new ConcatTask(new Inp2{x=new List<string>{"f","u","r","k","a","n"}});

            var matrix = new Matrix();
            matrix.nums = new int[100, 100];
            var matrixSum=new MatrixSum(matrix);
            
            //TestClient();
            
            m.Enqueue(mult.Serialize());
            m.Enqueue(mult2.Serialize());
            m.Enqueue(add.Serialize());
            m.Enqueue(swap.Serialize());
            m.Enqueue(concat.Serialize());
            m.Enqueue(matrixSum.Serialize());
            
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

            
            Console.WriteLine("tasks queued");

            
            Console.WriteLine("task will be dequed. Now cleaning task registry to simulate a server...");

            var registry = (ITaskRegistry) TaskRegistry.Instance;
            registry.Clear();
            registry.DiscoverTasks();

            for (long i = m.Length(); i > 0; i--)
            {
                string serialized = (string)m.Dequeue();
                Console.WriteLine($"dequed task: {serialized}");
            
                var task=new DefaultTaskDeserializer().Deserialize(serialized);
                Console.WriteLine("task is executing...");
                task.Execute();
                Console.WriteLine($"time elapsed: {task.GetTimeElapsed().Value.TotalMilliseconds} ms");
            }

            var x = "x";
        }
        
        static void TestTaskExecutingRedisQueue()
        {
            Console.WriteLine("Hello World!");
            ConnectionMultiplexer redis = ConnectionMultiplexer.Connect("localhost");
            var m=new RedisTaskQueue(redis);
            var mult=new MultiplyTask(new Inp{x=4,y=5});
            var mult2=new MultiplyTask(new Inp{x=4,y=56});
            var add=new AddTask(new (5,6));
            var swap=new SwapTask(new ("abi","naber"));
            var concat=new ConcatTask(new Inp2{x=new List<string>{"f","u","r","k","a","n"}});

            var matrix = new Matrix();
            matrix.nums = new int[100, 100];
            var matrixSum=new MatrixSum(matrix);
            
            //TestClient();
            
            m.Enqueue(mult);
            m.Enqueue(mult2);
            m.Enqueue(add);
            m.Enqueue(swap);
            m.Enqueue(concat);
            m.Enqueue(matrixSum);
            
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

            
            Console.WriteLine("tasks queued");

            
            Console.WriteLine("task will be dequed. Now cleaning task registry to simulate a server...");

            var registry = (ITaskRegistry) TaskRegistry.Instance;
            registry.Clear();
            registry.DiscoverTasks();

            for (long i = m.Length(); i > 0; i--)
            {
                var task = m.Dequeue();
                Console.WriteLine("task is executing...");
                task.Execute();
                Console.WriteLine($"time elapsed: {task.GetTimeElapsed().Value.TotalMilliseconds} ms");
            }

            var x = "x";
        }
        
        static void Main(string[] args)
        {
            if (args.Length > 0 && args[0]=="worker")
            {
                var dotq = new DotqApi();
                var worker = dotq.CreateWorker();
                worker.StartConsumerLoop(new TimeSpan(0, 0, 50));
            }
            if (args.Length > 0 && args[0]=="client-1")
            {
                var dotq = new DotqApi();
                var task = new AddTask((5, 5));
                var handle = dotq.Delay(task);
                handle.Wait();
                var result = handle.GetResult();
                Console.WriteLine($"Result :{result}");
            }
            if (args.Length > 0 && args[0]=="client-2")
            {
                var dotq = new DotqApi();
                var task = new AddTask((5, 5));
                var handle = dotq.Build(task, o => Console.WriteLine($"Result is {o}"));
                dotq.DelayHandle(handle);
                handle.Wait();
                var result = handle.GetResult();
                Console.WriteLine($"Result :{result}");
            }
            //TestApi.Test1();
            //TestTaskExecutingRedisQueue();
            //TestRedisPromise.StressTest();
            //TestRedisPromise.StressTestConcurrent();
            //TestRedisPromise.RedisPubSubExchangeConcurrent();
            //TestRedisPromise.RedisPubSubExchangeSequential();
            //TestRedisPromise.testRetry();
            //TestTaskResultHandle.ParallelTest();
            //TestTaskResultHandle.CreatePromiseTest();
        }
    }
}