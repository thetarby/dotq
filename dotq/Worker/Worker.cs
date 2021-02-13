using System;
using System.Diagnostics;
using dotq.Storage;
using dotq.Storage.RedisPromise;
using dotq.Task;

namespace dotq.Worker
{
    public class Worker : IWorker
    {
        private ITaskResultStore<string> resultStore;
        private IDotQueue<string> queue;
        private DefaultTaskDeserializer deserializer;
        private RedisPromiseServer promiseResolver;
        public Worker(IDotQueue<string> q, ITaskResultStore<string> r)
        {
            queue = q;
            resultStore = r;
            deserializer = new DefaultTaskDeserializer();
            promiseResolver = new RedisPromiseServer();
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
                var task = deserializer.Deserialize(queue.Dequeue());
                task.Execute();
                var res = task.SerializeResult();
                promiseResolver.Resolve(task.GetInstanceIdentifier(), res);
                resultStore.SetResultOfTask(task, res);
            }

            s.Stop();
        }
    }
}