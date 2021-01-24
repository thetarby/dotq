using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using ServiceStack;

namespace dotq.Storage.Pooling
{
    public abstract class GenericPool<T>
    {
        protected Queue<(T, DateTime)> Available;
        protected Dictionary<T, DateTime> InUse;
        private object lck=new object();
        private TimeSpan? _deadTime;

        public GenericPool(TimeSpan? deadTime)
        {
            Available = new Queue<(T, DateTime)>();
            InUse = new Dictionary<T, DateTime>();
            _deadTime = deadTime;
        }

        /// <summary>
        /// creates a new instance of resource T
        /// </summary>
        /// <returns></returns>
        protected abstract T Create();
        
        /// <summary>
        /// validates if resource T is still a valid instance. Defaults to returning true. Behaviour can be changed by overriding the method.
        /// </summary>
        /// <param name="resource"></param>
        /// <returns></returns>
        protected virtual bool Validate(T resource)
        {
            return true;
        }
        
        /// <summary>
        /// Called when resource is dead. (Popped from Available resources and not pushed to InUse resources). Defaults to noop. Behaviour can be changed by overriding the method.
        /// </summary>
        /// <param name="resource"></param>
        protected virtual void DisposeResource(T resource)
        {
            return;
        }
        
        public virtual T Borrow()
        {
            var now = DateTime.Now;
            lock (lck)
            {
                while (Available.Count>0) // this is O(1)
                {
                    var pair=Available.Dequeue();
                    T resource = pair.Item1;
                    var createTime = pair.Item2;
                    if (_deadTime==null || (now - createTime < _deadTime.Value))
                    {
                        // resource is not timed out
                        if (Validate(resource))
                        {
                            InUse.Add(resource, now);
                            return resource;   
                        }
                    }
                    
                    // if method does not return it means resource is popped and it is dead hence call dispose on it.
                    DisposeResource(resource);

                }
                // no objects available, create a new one 
                var createdResource = Create();
                InUse.Add(createdResource, now);
                return createdResource; 
            }
        }
        
        public virtual void Return(T resource)
        {
            lock (lck)
            {
                InUse.Remove(resource);
                Available.Enqueue((resource, DateTime.Now));
            }
        }
    }
} 