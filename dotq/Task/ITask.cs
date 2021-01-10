using System;
using System.Collections;
using System.Runtime.Serialization;

namespace dotq.Task
{
    public interface ITask
    {
        void Execute();
        
        Type GetTypeofTaskModel();
        
        Type GetTypeofOutput();
        
        string GetIdentifier();

        // GetIdentifier creates a unique id for the task type which means all instances of a dottask have the same id
        // GetInstanceIdentifier should produce a unique id for all instances of an ITask no matter what their derived classes are.
        // note that these ids should produce same result for same ITask instances in distributed workers hence something like a
        // guid would not work. 
        string GetInstanceIdentifier();

        string Serialize();

        string SerializeResult();
        
        object DeserializeResultToObject(string s);
        
        ITask Deserialize(string s);

        object GetObjectResult();

        //this is the time when client creates the task instance
        DateTime GetCreationTime();
        
        //this is the time when worker starts to executing the task
        DateTime? GetStartingTime();
        
        DateTime? GetEndingTime();
        
        TimeSpan? GetTimeElapsed();
    }
}