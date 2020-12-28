using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Net.Http;
using System.Threading;
using dotq.Client;
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

            var h = new LongPollingClient();
            var t=h.Get("http://slowwly.robertomurray.co.uk/delay/2900/url/https://jsonplaceholder.typicode.com/todos/1");
            for (int i = 0; i < 10; i++)
            {
                Console.WriteLine(t.Status);
                Thread.Sleep(500);
            }
            Console.WriteLine(t.Result);
            var t2 = h.GetAsString("http://slowwly.robertomurray.co.uk/delay/2900/url/https://jsonplaceholder.typicode.com/todos/1");
            for (int i = 0; i < 10; i++)
            {
                Console.WriteLine(t2.Status);
                Thread.Sleep(500);
            }
            Console.WriteLine(t2.Result);
            
            
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