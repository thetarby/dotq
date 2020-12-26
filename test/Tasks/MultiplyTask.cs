using System;
using dotq.Task;

namespace test.Tasks
{
    public class Inp
    {
        public int x { get; set; }
        public int y { get; set; }
    }
    
    public class MultiplyTask:DotTask<Inp,int>
    {
        public MultiplyTask(Inp arguments) : base(arguments)
        {
        }

        public MultiplyTask(object o) : base(o)
        {
        }
        
        public MultiplyTask():base()
        {
        }
        
        public override void Run(Inp args)
        {
            Console.WriteLine(args.x*args.y);
        }
    }
}