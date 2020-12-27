using System;
using System.Collections;
using System.Collections.Generic;
using dotq.Storage;
using dotq.Task;
using dotq.TaskRegistry;
using test.Tasks;

namespace test
{
   
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Hello World!");
            var m=new MemoryStorage();

            var mult=new MultiplyTask(new Inp{x=4,y=5});
            var mult2=new MultiplyTask(new Inp{x=4,y=56});
            var add=new AddTask(new (5,6));
            var swap=new SwapTask(new ("abi","naber"));
            var concat=new ConcatTask(new Inp2{x=new List<string>{"f","u","r","k","a","n"}});
            
            m.Enqueue(mult.Serialize());
            m.Enqueue(mult2.Serialize());
            m.Enqueue(add.Serialize());
            m.Enqueue(swap.Serialize());
            m.Enqueue(concat.Serialize());
            Console.WriteLine("tasks queued");

            
            Console.WriteLine("task will be dequed. Now cleaning task registry to simulate a server...");

            var registry = (ITaskRegistry) TaskRegistry.Instance;
            registry.Clear();
            registry.DiscoverTasks();

            for (int i = m.Count(); i > 0; i--)
            {
                string serialized = (string)m.Dequeue();
                Console.WriteLine($"dequed task: {serialized}");
            
                var task=new DefaultTaskDeserializer().Deserialize(serialized);
                Console.WriteLine("task is executing...");
                task.Execute();   
            }
        }
    }
}