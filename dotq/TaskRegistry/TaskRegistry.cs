using System;
using System.Collections.Generic;
using System.Linq;
using dotq.Task;

namespace dotq.TaskRegistry
{
    public sealed class TaskRegistry:ITaskRegistry
    {
        private Dictionary<string, Type> _tasks;
        
        private TaskRegistry()
        {
            _tasks = new Dictionary<string, Type>();
        }
        
        private static readonly Lazy<TaskRegistry> _lazy = new(() => new TaskRegistry());
        
        public static TaskRegistry Instance => _lazy.Value;

        public void RegisterTask(Type task)
        {
            var key = task.Namespace + task.Name;
            if(_tasks.ContainsKey(key)) throw new Exception("Task with the same identifier already exists");
            
            _tasks.Add(task.Namespace+task.Name, task);
        }

        public bool RegisterTaskIfNotExists(Type task)
        {
            var key = task.Namespace + task.Name;
            if (_tasks.ContainsKey(key)) return false;
            
            _tasks.Add(task.Namespace+task.Name, task);
            return true;
        }

        public Type GetTaskByName(string name)
        {
            return _tasks[name];
        }

        public void Clear()
        {
            _tasks.Clear();
        }
        
    }
}