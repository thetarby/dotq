using System;
using dotq.Task;

namespace test.Tasks
{
    public class SwapTask:DotTask<(string, string),(string,string)>
    {
        public SwapTask() :base(){}
        public SwapTask(object o) :base(o){}
        public SwapTask((string,string) args) :base(args){}
        public override (string, string) Run((string, string) args)
        {
            Console.WriteLine($"{args.Item2}, {args.Item1}");
            return new("x", "y");
        }
    }
}