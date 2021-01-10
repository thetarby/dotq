using System;
using dotq.Storage;

namespace dotq.TaskResultHandle
{
    public class BasicTaskResultHandle : ITaskResultHandle
    {
        private string id;
        private IResultStorage _resultStore;
        public object Result { get; set; }

        public BasicTaskResultHandle(string taskInstanceId, IResultStorage resultStore)
        {
            id = taskInstanceId;
            _resultStore = resultStore;
            Result = null;
        }


        public object GetResult()
        {
            return _resultStore.GetResultOfTaskAsync(id);
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
    }
}