using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using dotq.Storage;
using dotq.TaskRegistry;
using StackExchange.Redis;
using test.Tasks;
using Xunit;

namespace TestProject1
{
    [Collection("Sequential")]
    public class TaskExecuting
    {
        [Fact]
        public void Test_TaskEnqueueAndExecuting()
        {
            ConnectionMultiplexer redis = ConnectionMultiplexer.Connect("localhost");
            var m=new RedisTaskQueue(redis);
            m.Clear();
            var mult=new MultiplyTask(new Inp{x=4,y=5});
            var add=new AddTask(new (5,6));
            var concat=new ConcatTask(new Inp2{x=new List<string>(){"Hello", " ", "World", "!"}});
            
            m.Enqueue(mult);
            m.Enqueue(add);
            m.Enqueue(concat);

            var registry = (ITaskRegistry) TaskRegistry.Instance;
            registry.Clear();
            registry.DiscoverTasks();

            for (long i = 0; i < m.Length(); i++)
            {
                var task = m.Dequeue();
                task.Execute();
                if (i == 0)
                {
                    var res=(int)task.GetObjectResult();
                    Assert.Equal(20, res);    
                }
                if (i == 1)
                {
                    var res=(int)task.GetObjectResult();
                    Assert.Equal(11, res);    
                }
                if (i == 2)
                {
                    var res=(string)task.GetObjectResult();
                    Assert.Equal("Hello World!", res);    
                }
            }
            
        }
        
        [Fact]
        public void Test_TaskEnqueueAndExecutingParallel()
        {
            ConnectionMultiplexer redis = ConnectionMultiplexer.Connect("localhost");
            var m=new RedisTaskQueue(redis);
            m.Clear();
            var mult=new MultiplyTask(new Inp{x=4,y=5});
            var add=new AddTask(new (5,6));
            var concat=new ConcatTask(new Inp2{x=new List<string>(){"Hello", " ", "World", "!"}});

            for (int i = 0; i < 1000; i++)
            {
                m.Enqueue(mult);
                m.Enqueue(add);
                m.Enqueue(concat);
            }

            Action worker = () =>
            {

                try
                {
                    var task = m.Dequeue();
                    var taskType=TaskRegistry.Instance.GetTaskByName(task.GetIdentifier());
                
                    task.Execute();
                    if (taskType == typeof(MultiplyTask))
                    {
                        var res=(int)task.GetObjectResult();
                        Assert.Equal(20, res);    
                    }
                    if (taskType == typeof(AddTask))
                    {
                        var res=(int)task.GetObjectResult();
                        Assert.Equal(11, res);    
                    }
                    if (taskType == typeof(ConcatTask))
                    {
                        var res=(string)task.GetObjectResult();
                        Assert.Equal("Hello World!", res);    
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                }
            };
            
            var registry = (ITaskRegistry) TaskRegistry.Instance;
            registry.Clear();
            registry.DiscoverTasks();
            
            Parallel.Invoke(worker, worker, worker, worker, worker, worker);
            
        }
    }
}