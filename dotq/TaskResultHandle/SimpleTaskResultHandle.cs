using System;
using dotq.Storage;

namespace dotq.TaskResultHandle
{
    public class SimpleTaskResultHandle<TResult> : ITaskResultHandle<TResult>
    {
        private string id;
        private ITaskResultStore<string> _resultStore;
        public object Result { get; set; }

        public SimpleTaskResultHandle(string taskInstanceId, ITaskResultStore<string> resultStore)
        {
            id = taskInstanceId;
            _resultStore = resultStore;
            Result = null;
        }


        public TResult GetResult()
        {
            throw new NotImplementedException();
        }

        public string GetRawResult()
        {
            throw new NotImplementedException();
        }

        public string Serialize()
        {
            throw new NotImplementedException();
        }

        public void OnResolve(Action action)
        {
            throw new NotImplementedException();
        }

        public object GetObjectResult()
        {
            throw new NotImplementedException();
        }

        public string GetStringResult()
        {
            throw new NotImplementedException();
        }
    }
}