using UnityEngine;

namespace Unity.Tutorials.Core.Editor
{
    internal static class EditorFindObjectUtils
    {
        internal static T[] FindObjectsByType<T>() where T : Object
        {
#if UNITY_6000_4_OR_NEWER
            return Object.FindObjectsByType<T>();
#elif UNITY_2023_1_OR_NEWER
            return Object.FindObjectsByType<T>(FindObjectsSortMode.None);
#else
            return Object.FindObjectsOfType<T>();
#endif
        }
    }
}
