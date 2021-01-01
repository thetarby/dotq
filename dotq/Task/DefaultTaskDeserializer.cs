using System;
using dotq.TaskRegistry;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace dotq.Task
{
    public class DefaultTaskDeserializer:ITaskDeserializer
    {

        public string GetTaskIdentifier(string json)
        {
            
            var jObject=JObject.Parse(json);
            var jToken = jObject["TaskIdentifier"];
            if (jToken == null)
                throw new Exception("cannot deserialize task");
            
            return Convert.ToString(jToken);

        }
        
        public ITask Deserialize(string s)
        {
            var identifier = GetTaskIdentifier(s);
            
            var registry = (ITaskRegistry)TaskRegistry.TaskRegistry.Instance;
            
            var taskType=registry.GetTaskByName(identifier);
            var taskInstance = (ITask)Activator.CreateInstance(taskType);
            var taskModelType = taskInstance?.GetTypeofTaskModel();

            if (taskModelType == null) 
                throw new Exception($"Cannot deserialize task {s}");
            
            object obj = JsonConvert.DeserializeObject(s, taskModelType);
            
            if (obj == null) 
                throw new Exception($"Cannot deserialize task {s}");
            
            var task = (ITask)Activator.CreateInstance(taskType, obj);
            return task;
        }
    }
}