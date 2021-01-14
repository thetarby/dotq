using System;
using System.Threading;
using System.Threading.Tasks;
using dotq.Storage;
using dotq.Task;
using dotq.TaskResultHandle;
using StackExchange.Redis;
using test.Tasks;

namespace test
{
    public static class TestTaskResultHandle
    {
        public static void ParallelTest()
        {
            ConnectionMultiplexer redis = ConnectionMultiplexer.Connect("localhost");
            var m=new MemoryStorage();
            
            int taskCount = 100;
            ITask[] tasks = new ITask[taskCount];
            PromiseTaskResultHandle<int>[] handles = new PromiseTaskResultHandle<int>[taskCount];
            for (int i = 0; i < taskCount; i++)
            {
                var add=new AddTask(new (i, i+1));
                tasks[i] = add;
                var handle = new PromiseTaskResultHandle<int>(add, redis, (o) => Console.WriteLine($"result is ready: {o}"));
                add.BindPromise(handle.GetPromise());
                
                m.Enqueue(add.Serialize());
                handles[i] = handle;
            }
            
            var resolver = new RedisPromiseServer(redis);
            
            // this is simulating many workers are resolving promises. (executing tasks in this context)
            Parallel.ForEach(handles, ((handle) =>
            {
                var task = new DefaultTaskDeserializer().Deserialize((string)m.Dequeue());
                task.Execute();
                var res=task.SerializeResult();
                
                // thanks to BindPromise each task's instance id is also id of its related promise. So we can directly resolve related promise by using it.
                resolver.Resolve(task.GetInstanceIdentifier(), res);
            }));
            
            
            for (int i = 0; i < taskCount; i++)
            {
                int correctResult = i + i + 1;
                while (!handles[i].IsResolved())
                {
                    // it might not come from redis yet
                    Thread.Sleep(10);
                }
                var calculatedResult=(int) handles[i].GetObjectResult();
                if (correctResult != calculatedResult)
                    throw new Exception("wrong result");
            }
            Console.WriteLine("ParallelTest is Successful");
            
            Thread.Sleep(1000);
        }
        
        public static void SequentialTest()
        {
            ConnectionMultiplexer redis = ConnectionMultiplexer.Connect("localhost");
            var m=new MemoryStorage();
            
            int taskCount = 100;
            ITask[] tasks = new ITask[taskCount];
            PromiseTaskResultHandle<int>[] handles = new PromiseTaskResultHandle<int>[taskCount];
            for (int i = 0; i < taskCount; i++)
            {
                var add=new AddTask(new (i, i+1));
                tasks[i] = add;
                var handle = new PromiseTaskResultHandle<int>(add, redis, (o) => Console.WriteLine($"result is ready: {o}"));
                add.BindPromise(handle.GetPromise());
                
                m.Enqueue(add.Serialize());
                handles[i] = handle;
            }
            
            var resolver = new RedisPromiseServer(redis);
            
            // promises resolved in a sequential manner. like one worker is processing all of them
            for (int i = 0; i < taskCount; i++)
            {
                var task = new DefaultTaskDeserializer().Deserialize((string)m.Dequeue());
                task.Execute();
                var res=task.SerializeResult();
                
                // thanks to BindPromise each task's instance id is also id of its related promise. So we can directly resolve related promise by using it.
                resolver.Resolve(task.GetInstanceIdentifier(), res);
            }
            
            Thread.Sleep(1000);
        }

        public static void CreatePromiseTest()
        {
            ConnectionMultiplexer redis = ConnectionMultiplexer.Connect("localhost");
            var m=new MemoryStorage();
            
            int taskCount = 100;
            ITask[] tasks = new ITask[taskCount];
            PromiseTaskResultHandle<int>[] handles = new PromiseTaskResultHandle<int>[taskCount];
            for (int i = 0; i < taskCount; i++)
            {
                var add=new AddTask(new (i, i+1));
                tasks[i] = add;
                var client = RedisPromiseClientFactory.GetInstance(redis);
                var promise=client.CreatePromise();
                add.BindPromise(promise);
                var handle = new PromiseTaskResultHandle<int>(add, promise, (o) => Console.WriteLine($"result is ready: {o}"));
                
                //now promise will be ready to resolve
                client.Listen(promise);
                
                m.Enqueue(add.Serialize());
                handles[i] = handle;
            }
            
            var resolver = new RedisPromiseServer(redis);
            
            // this is simulating many workers are resolving promises. (executing tasks in this context)
            Parallel.ForEach(handles, ((handle) =>
            {
                var task = new DefaultTaskDeserializer().Deserialize((string)m.Dequeue());
                task.Execute();
                var res=task.SerializeResult();
                
                // thanks to BindPromise each task's instance id is also id of its related promise. So we can directly resolve related promise by using it.
                resolver.Resolve(task.GetInstanceIdentifier(), res);
            }));
            
            
            for (int i = 0; i < taskCount; i++)
            {
                int correctResult = i + i + 1;
                while (!handles[i].IsResolved())
                {
                    // it might not come from redis yet
                    Thread.Sleep(10);
                }
                var calculatedResult=(int) handles[i].GetObjectResult();
                if (correctResult != calculatedResult)
                    throw new Exception("wrong result");
            }
            Console.WriteLine("ParallelTest is Successful");
            
            Thread.Sleep(1000);
        }
    }
}