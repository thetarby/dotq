using Newtonsoft.Json;

namespace dotq.TaskResult
{
    public class BasicTaskResult
    {

        public object Result { get; set; }

        public BasicTaskResult()
        {
            Result = null;
        }

    }
}