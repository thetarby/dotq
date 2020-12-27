using System;
using dotq.TaskRegistry;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace dotq.Task
{
    public class DefaultTaskDeserializer:ITaskDeserializer
    {
        
        public ITask Deserialize(string s)
        {
            var jObject = JObject.Parse(s);
            var identifier = Convert.ToString(jObject["Identifier"]);
            
            var registry = (ITaskRegistry)TaskRegistry.TaskRegistry.Instance;
            
            var taskType=registry.GetTaskByName(identifier);
            var taskInstance = (ITask)Activator.CreateInstance(taskType);
            var serializeDtoType = taskInstance.GetSerializeDto();

            object obj = JsonConvert.DeserializeObject(s, serializeDtoType);
            
            //var obj=JsonSerializer.Deserialize(s);
            if (obj == null) throw new Exception("cannot deserialized the task");
            
            var task = (ITask)Activator.CreateInstance(taskType, obj);
            return task;
        }
    }
}