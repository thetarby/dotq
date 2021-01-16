using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace dotq.Storage
{
    public class MemoryQueue : ITaskQueue<string>
    {
        private Queue<string> _queue;
        
        public MemoryQueue()
        {
            _queue = new Queue<string>();
        }
        
        public void Enqueue(string o, int? priority = null)
        {
            lock (_queue)
            {
                _queue.Enqueue(o);
            }
            
        }

        public string Dequeue()
        {
            string res;
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

        public long Length()
        {
            return _queue.Count;
        }

        public void Clear()
        {
            throw new NotImplementedException();
        }

        public void PutData(string key, object value)
        {
            throw new NotImplementedException();
        }

        public object GetData(string key)
        {
            throw new NotImplementedException();
        }

        public object PopData(string key)
        {
            throw new NotImplementedException();
        }

        public bool In(string key)
        {
            throw new NotImplementedException();
        }
    }
}