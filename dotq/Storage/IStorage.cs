using System.Threading.Tasks;

namespace dotq.Storage
{
    public interface IStorage
    {
        void Enqueue(object o);
        
        object Dequeue();

        int Count();
    }
}