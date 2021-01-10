using System;
using dotq.Storage;
using dotq.Task;
using Newtonsoft.Json;
using StackExchange.Redis;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace dotq.TaskResultHandle
{
    
    
    /*
     * A task result handle is an object which is returned to client immediately after
     * it enqueues(executes, schedules) a task. It is responsible for getting the result itself,
     * revoking the task, setting an on resolve callback and things like that
     */
    public interface ITaskResultHandle
    {
        //will check and fetch result from ResultStorage
        // it returns object but client should already know the type hence can cast it to the proper type
        object GetObjectResult();

        string GetStringResult();
        
    }
    
    
    /*
     * Usage;
     * var add = new AddTask(new (5,6));
     * var resultHandle = new PromiseTaskResultHandle<TOutput>(add, redis, (o) => Console.WriteLine("result is ready"))
     */
    public class PromiseTaskResultHandle<TOutput> : ITaskResultHandle
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
            // check if promise is correctly configured with the task. Promise's internal id should be task id
            if (promise.GetInternalPromiseId() != task.GetIdentifier())
                throw new Exception("task and promise are not related to each other");
            _taskType = task.GetType();
            _promise = promise;
        }
        
        public PromiseTaskResultHandle(ITask task, ConnectionMultiplexer redis ,Action<object> onResolve=null)
        {
            // this ctor binds promise and tasks itself
            var key = task.GetInstanceIdentifier();
            var promiseClient = RedisPromiseClientFactory.GetInstance(redis);
            var promise = promiseClient.Listen(key);
            promise.OnResolve = onResolve;
            _promise = promise;
        }
        
        public Type GetTaskType() => _taskType;
        
        public Promise GetPromise() => _promise;
        
        public bool IsResolved() => _promise.IsResolved();
        
        TOutput GetResult() => JsonSerializer.Deserialize<TOutput>((string)_promise.Payload); // NOTE: since we are using json.net everywhere in task logic, promise payload will be a string. It is safe to cast to string.

        public object GetObjectResult()
        {
            return JsonSerializer.Deserialize((string) _promise.Payload, typeof(TOutput));
        }
        
        public string GetStringResult()
        {
            if (!_promise.IsResolved())
                throw new Exception("Promise Not Resolved");
            
            return (string)_promise.Payload;
        }
        

    }
}