namespace SerializableCallback
{
    /// <summary>
    /// https://github.com/Siccity/SerializableCallback
    /// </summary>
    /// <typeparam name="TReturn"></typeparam>
    public abstract class InvokableCallbackBase<TReturn>
    {
        /// <summary>
        /// https://github.com/Siccity/SerializableCallback
        /// </summary>
        /// <param name="args"></param>
        /// <returns>The return value of the callback</returns>
        public abstract TReturn Invoke(params object[] args);
    }
}
