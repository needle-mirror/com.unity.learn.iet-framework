using System;

namespace SerializableCallback
{
    /// <typeparam name="TReturn"></typeparam>
    [Serializable]
    public abstract class InvokableCallbackBase<TReturn>
    {
        /// <param name="args"></param>
        /// <returns>The return value of the callback</returns>
        public abstract TReturn Invoke(params object[] args);
    }
}
