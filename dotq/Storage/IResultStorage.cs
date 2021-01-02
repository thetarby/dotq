using dotq.Task;
using dotq.TaskResultHandle;


namespace dotq.Storage
{
    public interface IResultStorage
    {
        BasicTaskResult GetResultOfTaskAsync(ITask t);

        bool SetResultOfTask(ITask t);
    }
}