namespace dotq.TaskResult
{
    public interface ITaskResult<TOut>
    {
        //will check and fetch result from ResultStorage
        TOut GetResult();

        string GetRawResult();
    }
}