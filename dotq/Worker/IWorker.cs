using System;

namespace dotq.Worker
{
    public interface IWorker
    {
        public void StartConsumerLoop();
        
        public void StartConsumerLoop(DateTime start, DateTime end);

        public void StartConsumerLoop(TimeSpan duration);
    }
}