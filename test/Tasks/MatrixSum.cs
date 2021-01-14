using System;
using System.Globalization;
using System.Linq;
using System.Threading;
using dotq.Task;

namespace test.Tasks
{

    public class Matrix
    {
        public int[,] nums { get; set; }
    }
    
    public class MatrixSum: DotTask<Matrix,int>
    {
        public MatrixSum() :base(){}
        public MatrixSum(object o) :base(o){}
        public MatrixSum(Matrix args) :base(args){}
        
        
        public override int Run(Matrix args)
        {
            Console.WriteLine(args.nums.Cast<int>().Sum());
            return args.nums.Cast<int>().Sum();
        }
    }
    
}