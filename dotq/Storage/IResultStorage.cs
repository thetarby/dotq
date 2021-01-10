using dotq.Task;
using dotq.TaskResultHandle;


namespace dotq.Storage
{
    public interface IResultStorage
    {
        ITaskResultHandle GetResultOfTask(ITask t);
        
        ITaskResultHandle GetResultOfTask(string taskInstanceId);

        
        object GetRawResult(string taskInstanceId);
        
        
        object GetRawResult(ITask t);
        
        
        bool SetResultOfTask(ITask t);
    }
}