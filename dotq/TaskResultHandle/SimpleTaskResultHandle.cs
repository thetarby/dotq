using System;
using dotq.Storage;

namespace dotq.TaskResultHandle
{
    public class SimpleTaskResultHandle : ITaskResultHandle
    {
        private string id;
        private IResultStorage _resultStore;
        public object Result { get; set; }

        public SimpleTaskResultHandle(string taskInstanceId, IResultStorage resultStore)
        {
            id = taskInstanceId;
            _resultStore = resultStore;
            Result = null;
        }


        public object GetResult()
        {
            return _resultStore.GetResultOfTask(id);
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