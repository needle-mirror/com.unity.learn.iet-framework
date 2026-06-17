namespace SerializableCallback
{
    public abstract class InvokableEventBase
    {
        /// <param name="args"></param>
        public abstract void Invoke(params object[] args);
    }
}
