using System;
using System.Collections;
using System.Runtime.Serialization;

namespace dotq.Task
{
    public interface ITask
    {
        void Execute();
        
        Type GetTypeofTaskModel();
        
        string GetIdentifier();

        string Serialize();

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