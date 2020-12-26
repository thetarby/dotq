using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace dotq.Storage
{
    public class MemoryStorage : IStorage
    {
        private Queue<object> _queue;
        
        public MemoryStorage()
        {
            _queue = new Queue<object>();
        }
        
        public void Enqueue(object o)
        {
            lock (_queue)
            {
                _queue.Enqueue(o);
            }
            
            
        }

        public object Dequeue()
        {
            object res;
            try
            {
                lock (_queue)
                {
                    res=_queue.Dequeue();
                }
            }
            catch (System.InvalidOperationException e)
            {
                //TODO: blocking or what?
                Console.WriteLine(e);
                throw;
            }
            
            return res;
        }

        public int Count()
        {
            return _queue.Count;
        }
    }
}