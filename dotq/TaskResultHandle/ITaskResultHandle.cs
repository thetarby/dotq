using System;
using dotq.Storage;
using dotq.Task;
using Newtonsoft.Json;
using StackExchange.Redis;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace dotq.TaskResultHandle
{
    
    
    /*
     * A task result handle is an object which is returned to client immediately after
     * it enqueues(executes, schedules) a task. It is responsible for getting the result itself,
     * revoking the task, setting an on resolve callback and things like that
     */
    public interface ITaskResultHandle<TResultType>
    {
        //will check and fetch result from ResultStorage
        TResultType GetResult();

        string GetStringResult();
        
    }
}