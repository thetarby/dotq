# DotQ
DotQ is a lightweight distributed task queue written in .net5.0 which is heavily inspired by
huey and celery. It uses redis as its message broker.


## Requirements

* StackExhange.Redis
* Redis (as message broker)


## Usage

### Defining Tasks

First tasks should be created by inheriting from DotTask generic class. It has two type parameters
first of which is input to the task and second of which is the return value.

For instance an addition can be written like this;
```c#
public class ListSum : DotTask<IEnumerable<int>, int>
    {
        public ListSum() : base(){}
        public ListSum(object o) : base(o){}
        public ListSum(IEnumerable<int> args) : base(args){}

        public override int Run(IEnumerable<int> args)
        {
            int res = 0;
            foreach (var num in args)
            {
                res += num;
            }

            return res;
        }
    }
```
All three constructors(parameterless, constructor that only gets an object and constructor that only get input) 
should be defined and it is enough to only call base constructor. 

Run method is the only method which requires a concrete implementation. In this method input parameter is passed in
and it should return output type. This is where task logic should be defined in above case it is summation of input
parameters.

### Queueing Tasks
After creating a task we need to queue it to be able to execute it. To do that a DotqApi instance should be created.
To create a DotApi instance a ConnectionMultiplexer instance is needed which is basically a redis instance by 
StackExchange.Redis.


```c#
ConnectionMultiplexer redis = ConnectionMultiplexer.Connect("server1:6379,server2:6379");
var dotq = new DotqApi(redis);
```
or if you want to connect to a local redis instance on default redis port you can directly call constructor 
without any parameter; 
```c#
var dotq = new DotqApi(); // this connects to a redis instance on localhost port 6379
```

Now that we have a dotq instance we can execute tasks;
```c#
// first create an instance of the task with its inputs. 
var task = new ListSum(new List<int>{1,2,3,4,5,6,7,8,9,10});

// then pass it to dotq.Delay
var handle = dotq.Delay(task);
```

```dotq.Delay``` queues the task and immediately returns a task result handle which is a simple wrapper on task to 
access its result. Task result handle can be waited for task to return by calling wait on it;

```c#
// this will wait until task is executed by a worker process.
handle.wait()
```

After it is executed return value can be get like this;
```c#
int res = handle.GetResult(); // this will return 55
```

Alternatively you can create chains with tasks like this;
```c#

// Build method creates a handle but does not queue the task. It also gets an OnResolve handler
// which is run after a worker executes the task. You can write the answer to the console or you
// can send a new task which gets the first task's return value as its input.
var handle = dotq.Build(task, o => Console.WriteLine($"Result is {o}"));

var anotherHandle = dotq.Build(task, firstTaskResult =>
    {
        Console.WriteLine($"first is executed result: {firstTaskResult}");
        
        var handle = dotq.Build(new AddTask((firstTaskResult, 5)), res =>{
            Console.WriteLine($"Chain result is: {res}");
        });
        
        dotq.DelayHandle(handle);
    });
    
// this will queue the task.
dotq.DelayHandle(handle);

// this should first write 'first is executed result: 55'
// and then 'Chain result is: 60' to the console.
dotq.DelayHandle(anotherHandle);


```

But the thing is tasks will never be executed unless there is at least one worker process.

### Create a Worker Process
A worker process is process ideally on a different machine which pulls tasks queued by the client, executes them
and send back the result. It runs like a simple consumer loop.

To create a simple worker process;
```c#
var dotq = new DotqApi("server1:6379,server2:6379"); // connect to the same redis instance.
var worker = dotq.CreateWorker();
worker.StartConsumerLoop(new TimeSpan(0, 0, 50)); // this loop will run for at least 50 seconds.
```

Note that as in celery and huey tasks should be defined in the worker process too.


## Todos

- A better and intuitive api.
- Priority for tasks.
- Scheduling tasks.

License
----

MIT

