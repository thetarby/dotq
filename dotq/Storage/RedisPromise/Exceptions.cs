using System;

namespace dotq.Storage.RedisPromise
{
    public class PromiseIsAlreadyInMapperException : Exception {}
    
    public class PromiseIsAlreadyListeningOnAnotherClientException : Exception {}
    
    
    public class PromiseIsAlreadyResolvedException : Exception {}
}