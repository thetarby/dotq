using System;
using System.Collections;
using System.Runtime.Serialization;

namespace dotq.Task
{
    public interface ITask
    {
        void Execute();
        
        Type GetSerializeDto();
        
        string GetIdentifier();

        string Serialize();

        ITask Deserialize(string s);

        object GetObjectResult();
    }
}