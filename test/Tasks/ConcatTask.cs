﻿using System;
using System.Collections.Generic;
using dotq.Task;

namespace test.Tasks
{
    public class Inp2
    {
        public List<string> x { get; set; }
    }

    public class ConcatTask:DotTask<Inp2,string>
    {
        public ConcatTask(Inp2 arguments) : base(arguments)
        {
        }

        public ConcatTask(object o) : base(o)
        {
        }
    
        public ConcatTask():base()
        {
        }
    
        public override string Run(Inp2 args)
        {
            var concat = "";
            foreach (var arg in args.x)
            {
                concat += arg;
            }
            Console.WriteLine(concat);
            return concat;
        }
    }
    
}