namespace Unity.Tutorials.Editor
{
    internal class View
    {
        internal virtual string Name => string.Empty;
        protected TutorialWindow Application => TutorialWindow.Instance;

        public virtual void SubscribeEvents() { }
        public virtual void UnsubscribeEvents() { }
    }
}
