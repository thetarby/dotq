using System;
using System.Xml.Schema;
using dotq.Storage;
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
        private Type _taskType;

        public PromiseTaskResultHandle(ITask task, Promise promise)
        {
            // check if promise is correctly configured with the task. Promise's internal id should be task id
            if (promise.GetInternalPromiseId() != task.GetIdentifier())
                throw new Exception("task and promise are not related to each other");
            _taskType = task.GetType();
            _promise = promise;
        }
        
        public PromiseTaskResultHandle(ITask task, Promise promise, Action<object> onResolve)
        {
            // check if promise is correctly configured with the task. Promise's id should be task id
            if (promise.GetPromiseId() != task.GetInstanceIdentifier())
                throw new Exception("task and promise are not related to each other");
            _taskType = task.GetType();
            _promise = promise;
        }
        
        /// <summary>
        /// binds task and handle directly and starts to listen which means it should be used after task is enqueued.
        /// If this is used with a task which is not enqueued then it will use resources for a promise which cannot be resolved.
        /// </summary>
        /// <param name="task"></param>
        /// <param name="redis"></param>
        /// <param name="onResolve"></param>
        public PromiseTaskResultHandle(ITask task, ConnectionMultiplexer redis ,Action<object> onResolve=null, bool listenImmediately=false)
        {
            // this ctor binds promise and tasks itself
            var key = task.GetInstanceIdentifier();
            var promiseClient = RedisPromiseClientFactory.GetInstance(redis);
            var promise = promiseClient.Listen(key);
            promise.OnResolve = onResolve;
            _promise = promise;
        
            //TODO: use listenImmediately;
        }
        
        
        public Type GetTaskType() => _taskType;
        
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
        

    }
}