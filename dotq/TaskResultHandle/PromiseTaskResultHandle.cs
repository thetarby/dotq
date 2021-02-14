using System;
using System.Diagnostics;
using System.Xml.Schema;
using dotq.Storage;
using dotq.Storage.RedisPromise;
using dotq.Task;
using Newtonsoft.Json;
using StackExchange.Redis;

namespace dotq.TaskResultHandle
{
 
    /*
     * Usage;
     * var add = new AddTask(new (5,6));
     * var resultHandle = new PromiseTaskResultHandle<TOutput>(add, redis, (o) => Console.WriteLine("result is ready"))
     */
    public class PromiseTaskResultHandle<TResult> : ITaskResultHandle<TResult>
    {
        private Promise _promise;
        private ITask _task;
        private Type _taskType;
        private Action<object> _onResolve;

        public PromiseTaskResultHandle(ITask task, Promise promise, Action<object> onResolve=null)
        {
            // check if promise is correctly configured with the task. Promise's id should be task id
            if (promise.ParsePromiseId().Item2 != task.GetInstanceIdentifier())
                throw new Exception("task and promise are not related to each other");
            _task = task;
            _promise = promise;
        }
        
        
        /// <summary>
        /// OUTDATED: binds task and handle directly and starts to listen which means it should be used after task is enqueued.
        /// If this is used with a task which is not enqueued then it will use resources for a promise which cannot be resolved.
        /// </summary>
        /// <param name="task"></param>
        /// <param name="redis"></param>
        /// <param name="onResolve"></param>
        public PromiseTaskResultHandle(ITask task, ConnectionMultiplexer redis ,Action<object> onResolve=null, bool listenImmediately=false)
        {
            // this ctor binds promise and tasks itself
            var promiseClient = RedisPromiseClientFactory.GetInstance(redis);
            var promise = promiseClient.CreatePromise();
            promise.OnResolve = onResolve;
            _promise = promise;
            _task = task;
            //TODO: use listenImmediately;
        }


        public PromiseTaskResultHandle(ITask task, Action<object> onResolve = null)
        {
            _task = task;
            _onResolve = onResolve;
        }


        public void Listen(RedisPromiseClient promiseClient)
        {
            var promise = promiseClient.CreatePromise();
            _task.BindPromise(promise);
            promiseClient.Listen(promise);
                
            if (_onResolve != null)
                promise.OnResolve = _onResolve;
            _promise = promise;
        }
        
        
        public void Queue(IDotQueue<ITask> q)
        {
            q.Enqueue(_task);
        }
        
        
        public Type GetTaskType() => TaskRegistry.TaskRegistry.Instance.GetTaskByName(_task.GetIdentifier());


        public ITask GetTask() => _task;
        
        
        public Promise GetPromise() => _promise;
        
        
        public bool IsResolved() => _promise.IsResolved();
        
        
        public TResult GetResult() => JsonConvert.DeserializeObject<TResult>((string)_promise.Payload); // NOTE: since we are using json.net everywhere in task logic, promise payload will be a string. It is safe to cast to string.

        
        public object GetObjectResult()
        {
            return JsonConvert.DeserializeObject((string) _promise.Payload, typeof(TResult));
        }

        
        public string GetStringResult()
        {
            if (!_promise.IsResolved())
                throw new Exception("Promise Not Resolved");
            
            return (string)_promise.Payload;
        }

        public void Wait()
        {
            _promise.Wait();
        }
    }
}