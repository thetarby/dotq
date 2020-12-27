namespace dotq.Task
{
    public interface ITaskDeserializer
    {
        ITask Deserialize(string s);
    }
}