using System;
using System.Collections.Generic;

namespace dotq.Task
{
    
    // This simple generic class represents a task with its arguments.
    // This class has all the information to convert it to an DotTask instance and execute it.
    public class TaskModel<TArgs>
    {
        public string TaskIdentifier { get; set; }
        public TArgs Args { get; set; }

        public DateTime CreationTime { get; set; }
        public Dictionary<string, string> Options { get; set; }
    }
}