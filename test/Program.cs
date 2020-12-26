using System;
using System.Collections;
using System.Collections.Generic;
using dotq.Storage;
using dotq.Task;
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
            var concat=new ConcatTask(new Inp2{x=new List<string>{"f","u","r","k","a","n"}});
            
            m.Enqueue(mult.Serialize());
            m.Enqueue(mult2.Serialize());
            m.Enqueue(concat.Serialize());
            Console.WriteLine("tasks queued");

            
            for (int i = m.Count(); i > 0; i--)
            {
                Console.WriteLine("task will be dequed...");
                string serialized = (string)m.Dequeue();
                Console.WriteLine($"dequed task: {serialized}");
            
                var task=DotTask<string,string>.StaticDeserialize(serialized);
                Console.WriteLine("task is executing...");
                task.Execute();   
            }
        }
    }
}