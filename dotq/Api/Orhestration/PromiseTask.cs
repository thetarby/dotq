using System;
using dotq.Storage.RedisPromise;
using dotq.Task;

namespace dotq.Api.Orhestration
{
    // NOT USED
    public abstract class PromiseTask<TInput, TOutput> : DotTask<TInput, TOutput>
    {
        private readonly TInput _arguments;
        private readonly string _identifier;
        private string _instanceIdentifier;
        private TOutput _objectResult;
        private TaskStatus _status=TaskStatus.Pending;
        
        private DateTime _creationTime;
        private DateTime? _startingTime=null;
        private DateTime? _endingTime=null;
        
        private RedisPromiseClient _promiseClient;
        private Promise _promise;

        public PromiseTask(TInput arguments)
        {
            _creationTime=DateTime.Now.ToUniversalTime();
            _identifier = this.GetType().Namespace + this.GetType().Name;
            _arguments = arguments;
            var registry = TaskRegistry.TaskRegistry.Instance;
            registry.RegisterTaskIfNotExists(this.GetType());

            _promise = _promiseClient.CreatePromise();
            _instanceIdentifier = _promise.GetCompositeKey();
        }
        
        
        // This ctor called by deserializer to create task. object o is actually a TaskModel<TInput> instance
        public PromiseTask(object o)
        {
            var taskModel = ((TaskModel<TInput>) o);
            var arguments = taskModel.Args;
            _creationTime=taskModel.CreationTime;
            _identifier = this.GetType().Namespace + this.GetType().Name;
            _instanceIdentifier= taskModel.TaskInstanceIdentifier;
            _arguments = arguments;
            var registry = TaskRegistry.TaskRegistry.Instance;
            registry.RegisterTaskIfNotExists(this.GetType());
        }

        
        public PromiseTask() : base()
        {
            var registry = TaskRegistry.TaskRegistry.Instance;
            registry.RegisterTaskIfNotExists(this.GetType());
        }
    }
}