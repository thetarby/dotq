using dotq.Task;
using dotq.TaskResultHandle;
using ServiceStack;


namespace dotq.Storage
{
    
    //TODO: implement a class named TaskResult to abstract away TTaskResult type parameter and make ITaskResultStore a concrete type which implements IDataStore<TaskResult>.
    public interface ITaskResultStore<TTaskResult> : IDataStore<TTaskResult>
    {
        TTaskResult GetResultOfTask(ITask t);
        
        TTaskResult GetResultOfTask(string taskInstanceId);
        
        /// <summary>
        /// will execute task t and saves its result
        /// </summary>
        /// <param name="t"></param>
        /// <returns></returns>
        bool SetResultOfTask(ITask t);
        
        /// <summary>
        /// saves result as result of task t
        /// </summary>
        /// <param name="t"></param>
        /// <param name="result"></param>
        /// <returns></returns>
        bool SetResultOfTask(ITask t, TTaskResult result);
        
        /// <summary>
        /// saves result as result of task with instance identifier taskInstanceId
        /// </summary>
        /// <param name="taskInstanceId"></param>
        /// <param name="result"></param>
        /// <returns></returns>
        bool SetResultOfTask(string taskInstanceId, TTaskResult result);
        
    }
}