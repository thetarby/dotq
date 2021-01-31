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
        
        /// <summary>
        /// since a dottask should give same result when GetInstanceIdentifier is called after deserialization,
        /// json string should include TaskInstanceIdentifier field (Otherwise GetInstanceIdentifier creates a new guid and gives a different result)
        ///
        /// This method checks if TaskInstanceIdentifier is set in the json string. If not returns false.
        /// </summary>
        /// <param name="json"></param>
        /// <returns></returns>
        public bool IsValid(string json)
        {
            
            var jObject=JObject.Parse(json);
            var jToken = jObject["TaskInstanceIdentifier"];
            if (jToken == null)
                return false;
            return true;

        }
        
        public ITask Deserialize(string s)
        {
            if (!IsValid(s))
                throw new Exception("Task cannot be deserialized since it does not have taskInstanceIdentifier field");
            
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