using System;
using System.Collections.Generic;
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
            if(_tasks.ContainsKey(key)) return;
            
            _tasks.Add(task.Namespace+task.Name, task);
        }

        public Type GetTaskByName(string name)
        {
            return _tasks[name];
        }
    }
}