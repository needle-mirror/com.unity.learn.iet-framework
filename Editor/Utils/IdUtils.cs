using UnityEditor;
using UnityEngine;

namespace Unity.Tutorials.Editor
{
    internal static class IdUtils
    {
        // Required because GetInstanceID is deprecated from Unity 6.4, but GetEntityId doesn't exist before 6.3
        // TODO: Once the package is 6.3 and above-only, this entire class can be dismantled

#if UNITY_6000_3_OR_NEWER
        internal static EntityId GetIdFor(Object target) => target.GetEntityId();
        internal static Object IdToObject(EntityId entityId) => EditorUtility.EntityIdToObject(entityId);
        internal static bool IsIdNull(EntityId entityId) => entityId == EntityId.None;
        internal static EntityId NullId => EntityId.None;
#else
        internal static int GetIdFor(Object target) => target.GetInstanceID();
        internal static Object IdToObject(int instanceId) => EditorUtility.InstanceIDToObject(instanceId);
        internal static bool IsIdNull(int instanceId) => instanceId == 0;
        internal static int NullId => 0;
#endif
    }
}
