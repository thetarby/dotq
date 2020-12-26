using System;
using System.Collections;

namespace dotq.Task
{
    public interface ITask
    {
        void Execute();

        //IList GetArguments();

        Type GetSerializeDto();
        string GetIdentifier();

        string Serialize();

        ITask Deserialize(string s);
    }
}