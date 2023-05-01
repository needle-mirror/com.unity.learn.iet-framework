using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Unity.Tutorials.Core
{
    internal static class RuntimeFindObjectUtils
    {
        internal static T[] FindObjectsByTypeSorted<T>() where T : Object
        {
#if UNITY_2023_1_OR_NEWER
            return Object.FindObjectsByType<T>(FindObjectsSortMode.InstanceID);
#else
        // Earlier API didn't allow sorting mode to be controlled so always returns the array sorted by InstanceID
        return Object.FindObjectsOfType<T>();
#endif
        }

        internal static T[] FindObjectsByTypeUnsorted<T>() where T : Object
        {
#if UNITY_2023_1_OR_NEWER
            return Object.FindObjectsByType<T>(FindObjectsSortMode.None);
#else
        // Earlier API didn't allow sorting mode to be controlled so always returns the array sorted by InstanceID
        return Object.FindObjectsOfType<T>();
#endif
        }
    }
}
