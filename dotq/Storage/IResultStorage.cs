using dotq.Task;
using dotq.TaskResultHandle;


namespace dotq.Storage
{
    public interface IResultStorage
    {
        BasicTaskResultHandle GetResultOfTaskAsync(ITask t);
        
        BasicTaskResultHandle GetResultOfTaskAsync(string taskInstanceId);

        object GetRawResult(string taskInstanceId);
        
        object GetRawResult(ITask t);
        
        bool SetResultOfTask(ITask t);
    }
}