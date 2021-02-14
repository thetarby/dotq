using System;

namespace dotq.Worker
{
    public interface IWorker
    {
        /// <summary>
        /// starts consumer loop immediately and it runs forever.
        /// </summary>
        public void StartConsumerLoop();
        
        /// <summary>
        /// schedules consumer loop to run at start and stop at end datetime.
        /// </summary>
        /// <param name="start"></param>
        /// <param name="end"></param>
        public void StartConsumerLoop(DateTime start, DateTime end);

        /// <summary>
        /// runs task loop for duration.
        /// </summary>
        /// <param name="duration"></param>
        public void StartConsumerLoop(TimeSpan duration);
    }
}