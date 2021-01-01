using System.Reflection;

namespace dotq.Task
{
    public interface ISerializableTask<TArgs>
    {
        TaskModel<TArgs> ToModel();
    }
}