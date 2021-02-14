using System;
using System.Threading;
using System.Threading.Tasks;
using dotq.Storage;
using dotq.Storage.RedisPromise;
using dotq.Task;
using dotq.TaskResultHandle;
using StackExchange.Redis;
using test.Tasks;
using Xunit;


namespace TestProject1
{
    public class UnitTest1
    {

        [Fact]
        public void TaskExecutionTest()
        {
            int x=21231, y=5123123;
            var add=new AddTask(new (x,y));
            add.Execute();
            Assert.Equal(x+y,add.GetResult());
        }
        
        [Fact]
        public void ParallelUnitTest()
        {
            ConnectionMultiplexer redis = ConnectionMultiplexer.Connect("localhost");
            var m=new MemoryQueue();
            RedisPromiseClient promiseClient = new RedisPromiseClient(redis);
            
            int taskCount = 100;
            ITask[] tasks = new ITask[taskCount];
            PromiseTaskResultHandle<int>[] handles = new PromiseTaskResultHandle<int>[taskCount];
            for (int i = 0; i < taskCount; i++)
            {
                var add=new AddTask(new (i, i+1));
                tasks[i] = add;
                var handle = new PromiseTaskResultHandle<int>(add, redis);
                handle.Listen(promiseClient);
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
                Assert.Equal(correctResult, calculatedResult);
            }
        }
        
        [Fact]
        public void AlwaysPassing()
        {
            Assert.Equal(4, 4);
        }
    }
}