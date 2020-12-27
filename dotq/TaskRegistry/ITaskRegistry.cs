using System;
using System.Linq;
using dotq.Task;

namespace dotq.TaskRegistry
{
    public interface ITaskRegistry
    {
        void RegisterTask(Type task);
        
        //registers a task, should return true if it does not exist and registered successfully, should return false if it is already registered.
        bool RegisterTaskIfNotExists(Type task);

        Type GetTaskByName(string name);

        //only for test purposes
        void Clear();

        void DiscoverTasks()
        {
            var type = typeof(ITask);
            var types = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(s => s.GetTypes())
                .Where(p => type.IsAssignableFrom(p));

            foreach (var t in types)
            {
                RegisterTaskIfNotExists(t);
            }
        }
    }
}