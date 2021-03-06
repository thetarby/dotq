﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Dynamic;
using System.Reflection;
using System.Reflection.Metadata.Ecma335;
using System.IO;
using System.Net;
using dotq.Storage;
using dotq.Storage.RedisPromise;
using dotq.TaskResultHandle;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace dotq.Task
{
    enum TaskStatus
    {
        Pending,
        Executing,
        Executed
    }
    
    public abstract class DotTask<TInput, TOutput>: ITask, ISerializableTask<TInput>
    {
        private readonly TInput _arguments;
        private readonly string _identifier;
        private string _instanceIdentifier;
        private TOutput _objectResult;
        private TaskStatus _status=TaskStatus.Pending;
        
        private DateTime _creationTime;
        private DateTime? _startingTime=null;
        private DateTime? _endingTime=null;

        public DotTask(TInput arguments)
        {
            _creationTime=DateTime.Now.ToUniversalTime();
            _identifier = this.GetType().Namespace + this.GetType().Name;
            _arguments = arguments;
            var registry = TaskRegistry.TaskRegistry.Instance;
            registry.RegisterTaskIfNotExists(this.GetType());
        }
        
        
        // This ctor called by deserializer to create task. object o is actually a TaskModel<TInput> instance
        public DotTask(object o)
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

        
        public DotTask() : base()
        {
            var registry = TaskRegistry.TaskRegistry.Instance;
            registry.RegisterTaskIfNotExists(this.GetType());
        }
        
        
        public void Execute()
        {
            _startingTime = DateTime.Now.ToUniversalTime();
            _status = TaskStatus.Executing;
            TOutput res=Run(_arguments);
            this._objectResult = res;
            _endingTime = DateTime.Now.ToUniversalTime();
            _status = TaskStatus.Executed;
        }

        public Type GetTypeofTaskModel() => typeof(TaskModel<TInput>);

        public Type GetTypeofOutput() => typeof(TOutput);
        
        public TInput GetArguments() => _arguments;
        
        public string GetIdentifier() => _identifier;
        
        // NOTE: this should give the same result when a task is serialized and then deserialized.
        public string GetInstanceIdentifier()
        {
            if (_instanceIdentifier != null) 
                return _instanceIdentifier;
            Console.WriteLine("instance identifier is not set");
            return null;
        }
        
        /// <summary>
        /// sets task's instanceIdentifier to promise's internal_id so that when task is serialized and sent over network its promise can still be resolved by getTaskInstanceIdentifier()
        /// </summary>
        /// <param name="p"></param>
        /// <exception cref="Exception"></exception>
        public void BindPromise(Promise p)
        {
            if (_instanceIdentifier != null)
                throw new Exception("This task instance have already has an instanceIdentifier. Binding a promise will result in change of it, which is not possible");
            _instanceIdentifier = p.GetCompositeKey();
        }
        
        public string SerializeResult()
        {
            // TODO: maybe use a model here too to send some fixed meta data to the client about the execution
            if (_objectResult == null)
            {
                throw new Exception("This method cannot be called before executing task");
            }

            TOutput res = (TOutput) _objectResult;
            return JsonConvert.SerializeObject(res);
        }

        public TOutput DeserializeResult(string str)
        {
            return JsonSerializer.Deserialize<TOutput>(str);
        }
        
        public object DeserializeResultToObject(string str)
        {
            return JsonSerializer.Deserialize(str, GetTypeofOutput());
        }
        
        public string Serialize()
        {
            var taskModel = ToModel();
            var res= JsonConvert.SerializeObject(taskModel);
            return res;
        }
        
        public ITask Deserialize(string s)
        {
            return new DefaultTaskDeserializer().Deserialize(s);
        }

        public abstract TOutput Run(TInput args);

        public TOutput GetResult()
        {
            return (TOutput)_objectResult;
        }
        
        public object GetObjectResult()
        {
            return _objectResult;
        }

        public DateTime GetCreationTime()
        {
            return _creationTime;
        }

        public Nullable<DateTime> GetStartingTime()
        {
            return _startingTime;
        }

        public DateTime? GetEndingTime()
        {
            return _endingTime;
        }

        public TimeSpan? GetTimeElapsed()
        {
            if (_endingTime == null || _startingTime == null)
                return null;

            return _endingTime.Value.Subtract(_creationTime);
        }

        public bool IsExecuted()
        {
            return (_status == TaskStatus.Executed);
        }

        /// <summary>
        /// If a task is not bound to a promise its instanceId would be null and cannot be serialized. But a task does
        /// not have to have a promise. This method is called to assign a unique id to task when it is serialized
        /// if it is not bound to a promise already. 
        /// </summary>
        private string AssignUniqueId()
        {
            if (_instanceIdentifier != null)
            {
                throw new Exception("Instance Identifier should be null to call this method");
            }

            _instanceIdentifier = Guid.NewGuid().ToString();
            return _instanceIdentifier;
        }
        
        public TaskModel<TInput> ToModel()
        {
            return new TaskModel<TInput>
            {
                TaskIdentifier = GetIdentifier(),
                TaskInstanceIdentifier = GetInstanceIdentifier() ?? AssignUniqueId(),
                Args = _arguments,
                CreationTime = _creationTime,
                Options = new Dictionary<string, string>() //TODO: empty for now
            };
        }
    }
}