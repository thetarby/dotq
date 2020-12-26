using System;
using dotq.Task;

namespace dotq.TaskRegistry
{
    public interface ITaskRegistry
    {
        void RegisterTask(Type task);

        Type GetTaskByName(string name);
    }
}