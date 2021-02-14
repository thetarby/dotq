using System;
using System.Text.Json;
using System.Threading;
using StackExchange.Redis;

namespace dotq.Storage.RedisPromise
{
    /// <summary>
    /// Very similar to RedisPromiseClient but this implementation handles promises concurrently and asynchronously.
    /// Promises will not be resolved at the same order they are received.
    /// </summary>
    public class ConcurrentRedisPromiseClient : RedisPromiseClient
    {
        private ISubscriber _subscriber;

        public ConcurrentRedisPromiseClient(ConnectionMultiplexer redis) : base(redis)
        {
            _subscriber = null;
        }

        private Thread CloseConnectionThread(Semaphore luck, float timeoutInSeconds = -1,
            Promise promise = null)
        {
            if (promise == null && timeoutInSeconds != -1)
                throw new Exception("if timeout is specified a promise should be passed");
            
            Thread thread = new Thread(new ParameterizedThreadStart(CloseConnectionThread));
            thread.Start((object)(luck, timeoutInSeconds, promise));
            return thread;
        }

        protected override void CloseConnectionThread(object o)
        {
            var args = ((Semaphore, float, Promise)) o;
            var promiseTimeoutInSeconds = args.Item2;
            var promise = args.Item3;
            var l = args.Item1;

            if (promiseTimeoutInSeconds == -1)
            {
                l.WaitOne();
            }
            else
            {
                // TODO: this does not work now. It was working for per promise connections
                l.WaitOne((int)(promiseTimeoutInSeconds * 1000));
                promise.TimedOut();
            }
            
            Console.WriteLine("ALL PROMISES RESOLVED UNSUBSCRIBING...");
            _subscriber.Unsubscribe(_guid.ToString());
            _subscriber = null;
            canContinue.Release();
        }

        protected override void SetupPubSubServer()
        {
            lock (mapLock)
            {
                if (this._subscriber == null)
                {
                    var subscriber = _redis.GetSubscriber();
            
                    subscriber.Subscribe(_guid.ToString(), async (channel, message) => OnMessageHandler(message));
                    
                    CloseConnectionThread(canClose); // this starts thread
                    _subscriber = subscriber;
                    Thread.Sleep(1000); //give some time to establish subscription
                    
                }
            }
        }
    }


    public class PersistentRedisPromiseClient : RedisPromiseClient, IDisposable
    {
        public PersistentRedisPromiseClient(ConnectionMultiplexer redis) : base(redis)
        {
        }
        
        protected override void CloseConnectionThread(object o)
        {
            // does not close connection. When not needed anymore this client should be disposed with its subscriptions explicitly.
            Console.WriteLine("Closing Connection. But not really :)");
            canContinue.Release();
            return;
        }

        public void Dispose()
        {
            _channelSubscription?.Unsubscribe();
        }
    }
}