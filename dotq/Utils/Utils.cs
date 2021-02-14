using System;
using System.Collections.Generic;
using System.Threading;
using StackExchange.Redis;

namespace dotq.Utils
{
    public static class LocalRedis
    {
        private static readonly Lazy<ConnectionMultiplexer> _lazy = new(() =>  ConnectionMultiplexer.Connect("localhost"));
    
        public static ConnectionMultiplexer Instance => _lazy.Value;
    }
 
    
    /// <summary>
    /// Simple retry logic which can exponentially increase sleeping time after each retry. Usage;
    /// SimpleRetry.ExponentialDo(() =>{ return DoSomethingThatCanThrowException() }, TimeSpan.FromSeconds(0.1));
    /// wait times will be: 0.1 => 0.2 => 0.4 => 0.8 
    /// </summary>
    public static class SimpleRetry
    {
        public static void Do(
            Action action,
            TimeSpan retryInterval,
            int maxAttemptCount = 3)
        {
            Do<object>(() =>
            {
                action();
                return null;
            }, retryInterval, maxAttemptCount);
        }

        public static void ExponentialDo(
            Action action,
            TimeSpan firstRetrySpan,
            int maxAttemptCount = 5)
        {
            for (int i = 0; i < maxAttemptCount; i++)
            {
                Do<object>(() =>
                {
                    action();
                    return null;
                }, Math.Pow(2, i) * firstRetrySpan, 1);   
            }
        }

        public static T Do<T>(
            Func<T> action,
            TimeSpan retryInterval,
            int maxAttemptCount = 3)
        {
            var exceptions = new List<Exception>();

            for (int attempted = 0; attempted < maxAttemptCount; attempted++)
            {
                try
                {
                    if (attempted == 0) // in first attempt dont sleep
                        return action();
                    else
                        Thread.Sleep(retryInterval);
                }
                catch (Exception ex)
                {
                    exceptions.Add(ex);
                }
            }
            throw new AggregateException(exceptions);
        }
    }
}