using UnityEditor;

namespace Unity.Tutorials.Core.Editor
{
    internal class UsabilityAnalyticsProxy
    {
        public static void SendEvent(string eventType, System.DateTime startTime, System.TimeSpan duration, bool isBlocking, object parameters)
        {
            UsabilityAnalytics.SendEvent(eventType, startTime, duration, isBlocking, parameters);
        }
    }
}
