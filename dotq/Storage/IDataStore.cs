namespace dotq.Storage
{
    public interface IDataStore<TData>
    {
        
        /// <summary>
        /// Removes all elements in the store
        /// </summary>
        void Clear();

        /// <summary>
        /// Store an arbitrary key value pair.
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value">value should be serializable</param>
        void PutData(string key, TData value);

        /// <summary>
        /// Get value with key without removing the key
        /// </summary>
        /// <param name="key"></param>
        TData GetData(string key);
        
        /// <summary>
        /// Get value with key and then remove the key from store
        /// </summary>
        /// <param name="key"></param>
        TData PopData(string key);
        
        /// <summary>
        /// Check if given key exists in the store
        /// </summary>
        /// <param name="key"></param>
        /// <returns>True if key exists false otherwise</returns>
        bool In(string key);
    }
}