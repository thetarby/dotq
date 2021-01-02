namespace dotq.TaskResultHandle
{
    public class BasicTaskResult
    {
        private string id;
        public object Result { get; set; }

        public BasicTaskResult(string taskInstanceId)
        {
            id = taskInstanceId;
            Result = null;
        }
        
        
    }
}