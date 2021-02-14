using System.Collections.Generic;
using dotq.Task;

namespace test.Tasks
{
    public class ListSum: DotTask<IEnumerable<int>, int>
    {
        public ListSum() :base(){}
        public ListSum(object o) :base(o){}
        public ListSum(IEnumerable<int> args) :base(args){}


        public override int Run(IEnumerable<int> args)
        {
            int res = 0;
            foreach (var num in args)
            {
                res += num;
            }

            return res;
        }
    }
}