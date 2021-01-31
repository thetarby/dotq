using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using dotq.Storage;
using dotq.Storage.RedisStructures;
using dotq.Task;
using dotq.TaskResultHandle;
using StackExchange.Redis;
using test.Tasks;
using Xunit;

namespace TestProject1
{
    public class RedisStructureTest
    {
        [Fact]
        public void RedisMaxQueue_CheckAllEnqueuedArDequeued()
        {
            ConnectionMultiplexer redis = ConnectionMultiplexer.Connect("localhost");
            var q = new RedisMaxQueue(redis.GetDatabase(), "maxQueue"+Guid.NewGuid());
            var arr = new int[1000];
            for (int i = 0; i < arr.Length; i++)
            {
                arr[i] = i;
            }
            
            // many parallel processes enqueues at the same time
            Parallel.ForEach(arr, ((num) =>
            {
                q.Enqueue(num.ToString(), num);
            }));
            
            Thread.Sleep(1000);
            var resultArr = new Double[1000];
            
            // many parallel processes dequeues at the same time
            Parallel.ForEach(arr, ((num) =>
            {
                var x=q.Dequeue();
                resultArr[(int)x.Value.Score] = x.Value.Score;
            }));
            
            q.Clear();
            
            for (int i = 0; i < 1000; i++)
            {
                Assert.Equal((double)i, resultArr[i]);
            }
            
        }
        
        [Fact]
        public void RedisMaxQueue_CheckFirstHighestPriorityDequeued()
        {
            ConnectionMultiplexer redis = ConnectionMultiplexer.Connect("localhost");
            var q = new RedisMaxQueue(redis.GetDatabase(), "maxQueue"+Guid.NewGuid());
            for (int i = 0; i < 1000; i++)
            {
                q.Enqueue(i,i);
            }
            
            var x=q.Dequeue();
            
            Assert.Equal(999, x.Value.Score);
            q.Clear();
        }
    }
}