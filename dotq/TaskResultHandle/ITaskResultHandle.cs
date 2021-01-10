using System;

namespace dotq.TaskResultHandle
{
    
    
    /*
     * A task result handle is an object which is returned to client immediately after
     * it enqueues(executes, schedules) a task. It is responsible for getting the result itself,
     * revoking the task, setting an on resolve callback and things like that
     */
    public interface ITaskResultHandle
    {
        //will check and fetch result from ResultStorage
        // it returns object but client should already know the type hence can cast it to the proper type
        object GetResult();

        string GetRawResult();
        
        string Serialize();

        void OnResolve(Action action);
    }
}