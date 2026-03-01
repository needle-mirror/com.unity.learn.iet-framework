using UnityEditor;
using UnityEngine;
using UnityObject = UnityEngine.Object;

namespace Unity.Tutorials.Editor
{
    internal class InstantiatePrefabCriterionDrawers
    {
        [CustomPropertyDrawer(typeof(InstantiatePrefabCriterion.FuturePrefabInstance))]
        private class FuturePrefabInstanceDrawer : PropertyDrawer
        {
            private const string k_PrefabParentPropertyPath = "m_PrefabParent";

            public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
            {
                SerializedProperty prefabParentProperty = property.FindPropertyRelative(k_PrefabParentPropertyPath);
                return EditorGUI.GetPropertyHeight(prefabParentProperty);
            }

            public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
            {
                position.height = GetPropertyHeight(property, label);

                SerializedProperty prefabParentProperty = property.FindPropertyRelative(k_PrefabParentPropertyPath);
                Object obj = prefabParentProperty.objectReferenceValue;

                EditorGUI.BeginProperty(position, GUIContent.none, prefabParentProperty);
                EditorGUI.BeginChangeCheck();

                Object newObj = EditorGUI.ObjectField(position, obj, typeof(UnityObject), true);

                if (EditorGUI.EndChangeCheck())
                {
                    // Replace prefab instance with its prefab parent
                    if (newObj != null && PrefabUtility.GetPrefabInstanceStatus(newObj) != PrefabInstanceStatus.NotAPrefab)
                        newObj = PrefabUtilityShim.GetCorrespondingObjectFromSource(newObj);

                    prefabParentProperty.objectReferenceValue = newObj;
                }
                EditorGUI.EndProperty();

                position.y += position.height + EditorGUIUtility.standardVerticalSpacing;
            }
        }
    }
}
