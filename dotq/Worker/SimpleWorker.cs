using System;
using System.Diagnostics;
using System.Threading;
using dotq.Api;
using dotq.Storage;
using dotq.Storage.RedisPromise;
using dotq.Task;
using dotq.TaskRegistry;

namespace dotq.Worker
{
    public class SimpleWorker : IWorker
    {
        private ITaskResultStore<string> resultStore;
        private IDotQueue<ITask> queue;
        private DefaultTaskDeserializer deserializer;
        private RedisPromiseServer promiseResolver;
        private ITaskRegistry _taskRegistry;
        
        public SimpleWorker(IDotQueue<ITask> q, ITaskResultStore<string> r)
        {
            queue = q;
            resultStore = r;
            deserializer = new DefaultTaskDeserializer();
            promiseResolver = new RedisPromiseServer();
            _taskRegistry = TaskRegistry.TaskRegistry.Instance;
            _taskRegistry.DiscoverTasks();
        }

        public void StartConsumerLoop()
        {
            /* while(true):
             *      var task_str=q.Dequeue()
             *      var task=deserializer.Deserialize(task_str)
             *      task.Execute()
             *      var res_str=task.SerializeResult()
             *      r.PushResult(res_str)
             */
        }

        public void StartConsumerLoop(DateTime start, DateTime end)
        {
            throw new NotImplementedException();
        }

        public void StartConsumerLoop(TimeSpan duration)
        {
            Stopwatch s = new Stopwatch();
            s.Start();
            
            while (s.Elapsed < TimeSpan.FromSeconds(duration.Seconds))
            {
                ITask task;
                try
                {
                    task = queue.Dequeue();
                }
                catch (QueueIsEmptyException e)
                {
                    Console.WriteLine("queue is empty sleeping one sec");
                    Thread.Sleep(1000);
                    continue;
                }
                
                task.Execute();
                var res = task.SerializeResult();
                if(task.IsPromise())
                    promiseResolver.Resolve(task.GetInstanceIdentifier(), res);
                resultStore.SetResultOfTask(task, res);
            }
            
            Console.WriteLine("Time is out. Worker is stopping...");
            
            s.Stop();
        }
    }
}