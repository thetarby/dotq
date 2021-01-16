using System;
using System.Threading.Tasks;

namespace dotq.Storage
{
    public interface ITaskQueue<TInput>
    {
        void Enqueue(TInput o, int? priority=null);
        
        TInput Dequeue();
        
        long Length();

        void Clear();
    }
}