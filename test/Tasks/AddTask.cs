using System;
using dotq.Task;

namespace test.Tasks
{
    public class AddTask: DotTask<(int,int),int>
    {
        public AddTask() :base(){}
        public AddTask(object o) :base(o){}
        public AddTask((int,int) args) :base(args){}
        
        
        public override int Run((int, int) args)
        {
            Console.WriteLine(args.Item1 + args.Item2);
            return 0;
        }
    }
}