using System;
using System.Threading.Tasks;

namespace dotq.Storage
{
    public interface IDotQueue<TInput>
    {
        void Enqueue(TInput o, int? priority=null);
        
        TInput Dequeue();
        
        long Length();

        void Clear();

        bool IsEmpty();
    }
}