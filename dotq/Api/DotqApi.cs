using System;
using dotq.Api.Orhestration;
using dotq.Storage;
using dotq.Storage.RedisPromise;
using dotq.Task;
using dotq.TaskRegistry;
using dotq.TaskResultHandle;
using dotq.Worker;
using StackExchange.Redis;

namespace dotq.Api
{
    public class DotqApi
    {
        internal readonly ConnectionMultiplexer _redis;
        internal readonly IDotQueue<ITask> _taskQueue;
        internal readonly ITaskResultStore<string> _resultStore;
        internal readonly ITaskRegistry _taskRegistry;
        internal readonly RedisPromiseClient _redisPromiseClient;
        internal readonly RedisPromiseServer _redisPromiseServer;
        
        public DotqApi(ConnectionMultiplexer redis=null)
        {
            _redis = redis ?? LocalRedis.Instance;
            _taskQueue = new RedisTaskQueue(_redis);
            _resultStore = new SimpleRedisTaskResultStore(_redis);
            _taskRegistry = TaskRegistry.TaskRegistry.Instance;
            _redisPromiseClient = PromiseClientProvider.GetInstance(_redis);
            _redisPromiseServer = PromiseServerProvider.GetInstance();
        }

        public IWorker CreateWorker()
        {
            return new SimpleWorker(_taskQueue, _resultStore);
        }
    }
    
    
    public static class DotqApiExtensions
    {
        /// <summary>
        /// First creates promise and wraps it with a taskResultHandle.
        /// Then starts to listen for promise to resolve.
        /// Then enqueues task for a worker to execute task and resolve the promise.
        /// </summary>
        /// <param name="dotqApi"></param>
        /// <param name="task"></param>
        /// <typeparam name="TIn"></typeparam>
        /// <typeparam name="TOut"></typeparam>
        /// <returns></returns>
        public static ITaskResultHandle<TOut> Delay<TIn, TOut>(this DotqApi dotqApi, DotTask<TIn, TOut> task) // this method is written as extension method since I do not want DotqApi to be a generic class. It should be concrete
        {
            var handle = new PromiseTaskResultHandle<TOut>(task);
            handle.Listen(dotqApi._redisPromiseClient);
            dotqApi._taskQueue.Enqueue(task);
            return handle;
        }
        
        
        public static PromiseTaskResultHandle<TOut> Build<TIn, TOut>(this DotqApi dotqApi, DotTask<TIn, TOut> task, Action<TOut> onResolve=null)
        {
            var handle = new PromiseTaskResultHandle<TOut>(task, onResolve);
            return handle;
        }
        

        public static PromiseTaskResultHandle<TOut> DelayHandle<TOut>(this DotqApi dotqApi, PromiseTaskResultHandle<TOut> handle)
        {
            handle.Listen(dotqApi._redisPromiseClient);
            handle.Queue(dotqApi._taskQueue);
            return handle;
        }
        
        
        public static ITaskResultHandle<TOut> Delay2<TOut, TIn>(this DotqApi dotqApi, DotTask<TIn, TOut> task)
        {
            dotqApi._taskQueue.Enqueue(task);
            
            return new PromiseTaskResultHandle<TOut>(task, dotqApi._redis);
        }
    }


    public static class DotTaskExtensions
    {
        /// <summary>
        /// Util method to check if task has a promise waiting for its result.
        /// TODO: This is ugly and not good refactor is needed.
        /// </summary>
        /// <param name="task"></param>
        /// <returns></returns>
        public static bool IsPromise(this ITask task)
        {
            return task.GetInstanceIdentifier().Contains(":");
        }
    }
    
    
    public static class TaskResultHandleExtensions
    {
    }
}