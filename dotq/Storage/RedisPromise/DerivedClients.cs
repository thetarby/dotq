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

        private void CloseConnectionThread(object o)
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
                    Semaphore luck = new Semaphore(0,1);
                    
                    var subscriber = _redis.GetSubscriber();
            
                    subscriber.Subscribe(_guid.ToString(), async (channel, message) => {
                        countTime++;
                        lock (mapLock)
                        {
                            var promiseIdActualMessageDto =
                                JsonSerializer.Deserialize<PromiseIdActualMessageDto>(message.ToString());
                            var promiseId = promiseIdActualMessageDto.PromiseId;
                            var realmsg = promiseIdActualMessageDto.ActualMessage;

                            if (_mapper.ContainsKey(promiseId))
                            {
                                var promise = _mapper[promiseId];
                                _mapper.Remove(promiseId);
                                promise.Payload = realmsg;
                                promise._OnResolveBase();
                                if (_mapper.Count == 0)
                                {
                                    luck.Release();
                                    // to prevent another thread which calls GetPubSubServer to start a subscription while we are closing it, wait until closing thread cleans subscription before releasing mapLock
                                    canContinue.WaitOne();
                                }
                            }
                        }
                    });
                    
                    CloseConnectionThread(luck); // this starts thread
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