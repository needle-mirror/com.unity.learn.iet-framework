using UnityEditor;
using UnityEngine;

namespace Unity.Tutorials.Editor
{
    internal class PrefabUtilityShim
    {
        public static Object GetCorrespondingObjectFromSource(Object source)
        {
#if UNITY_2018_2_OR_NEWER
            return PrefabUtility.GetCorrespondingObjectFromSource(source);
#else
            return PrefabUtility.GetPrefabParent(source);
#endif
        }
    }
}
