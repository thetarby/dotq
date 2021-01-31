using dotq.Api.Orhestration;
using dotq.Storage;
using dotq.Storage.RedisPromise;
using dotq.Task;
using dotq.TaskRegistry;
using dotq.TaskResultHandle;
using Microsoft.VisualBasic;
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
            this._redis = redis ?? LocalRedis.Instance;
            _taskQueue = new RedisTaskQueue(_redis);
            _resultStore = new SimpleRedisTaskResultStore(_redis);
            _taskRegistry = TaskRegistry.TaskRegistry.Instance;
            _redisPromiseClient = PromiseClientProvider.GetInstance(_redis);
            _redisPromiseServer = PromiseServerProvider.GetInstance();
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
        public static ITaskResultHandle<TOut> Delay<TIn, TOut>(this DotqApi dotqApi, DotTask<TIn, TOut> task)
        {
            var key = task.GetInstanceIdentifier();
            var promiseClient = dotqApi._redisPromiseClient;
            
            // TODO: there is a potential problem here. Before listening for the promise, handle should be created. Change promise api accordingly
            var promise = promiseClient.Listen(key);
            var handle = new PromiseTaskResultHandle<TOut>(task, promise);
            
            dotqApi._taskQueue.Enqueue(task);
            return handle;
        }
        
        public static ITaskResultHandle<TOut> Delay2<TOut, TIn>(this DotqApi dotqApi, DotTask<TIn, TOut> task)
        {
            dotqApi._taskQueue.Enqueue(task);
            
            return new PromiseTaskResultHandle<TOut>(task, dotqApi._redis);
        }
    }
}