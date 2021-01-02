using dotq.Storage;
using dotq.Task;

namespace dotq.Worker
{
    public class Worker
    {
        private IResultStorage resultStore;
        private IStorage queue;
        
        public Worker(IStorage q, IResultStorage r)
        {
            queue = q;
            resultStore = r;
            DefaultTaskDeserializer deserializer = new DefaultTaskDeserializer();
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
    }
}