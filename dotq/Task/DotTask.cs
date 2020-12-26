using System;
using System.Collections;
using System.Reflection;
using System.Reflection.Metadata.Ecma335;
using System.IO;
using System.Net;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace dotq.Task
{
    public abstract class DotTask<TInput, TOutput>: ITask
    {
        private readonly TInput _arguments;
        private readonly string _identifier;
        public DotTask(TInput arguments)
        {
            _identifier = this.GetType().Namespace + this.GetType().Name;
            _arguments = arguments;
            var registry = TaskRegistry.TaskRegistry.Instance;
            registry.RegisterTask(this.GetType());
        }

        public DotTask(object o)
        {
            var arguments = ((SerializeDto) o).Arguments;
            _identifier = this.GetType().Namespace + this.GetType().Name;
            _arguments = arguments;
            var registry = TaskRegistry.TaskRegistry.Instance;
            registry.RegisterTask(this.GetType());
        }
        
        public DotTask(){}
        
        public void Execute()
        {
            Run(_arguments);
        }

        public Type GetSerializeDto()
        {
            return typeof(SerializeDto);
        }

        public TInput GetArguments()
        {
            return _arguments;
        }

        string ITask.GetIdentifier()
        {
            return _identifier;
        }

        public string Serialize()
        {
            var obj = new SerializeDto
            {
                Identifier = _identifier,
                Arguments = _arguments
            };
            return JsonSerializer.Serialize(obj);
        }
        
        public ITask Deserialize(string s)
        {
            return DotTask<TInput, TOutput>.StaticDeserialize(s);
        }
        
        public static ITask StaticDeserialize(string s)
        {
            var jObject = JObject.Parse(s);
            var identifier = Convert.ToString(jObject["Identifier"]);
            
            var registry = TaskRegistry.TaskRegistry.Instance;
            var taskType=registry.GetTaskByName(identifier);
            var taskInstance = (ITask)Activator.CreateInstance(taskType);
            var serializeDtoType = taskInstance.GetSerializeDto();

            object obj = JsonConvert.DeserializeObject(s, serializeDtoType);
            
            //var obj=JsonSerializer.Deserialize(s);
            if (obj == null) throw new Exception("cannot deserialized the task");
            
            var task = (ITask)Activator.CreateInstance(taskType, obj);
            
            return task;
        }
        

        public abstract void Run(TInput args);

        public class SerializeDto
        {
            public string Identifier { get; set; }
            public TInput Arguments { get; set; }
        }
    }
}