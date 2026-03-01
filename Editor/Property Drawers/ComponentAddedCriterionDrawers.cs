using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.Tutorials.Editor
{
    internal class ComponentAddedCriterionDrawers
    {
        [CustomPropertyDrawer(typeof(ComponentAddedCriterion.TypeAndFutureReference))]
        private class TypeAndFutureReferenceDrawer : PropertyDrawer
        {
            private static string s_SerializedTypeField = "SerializedType";

            public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
            {
                SerializedProperty serializedTypeProperty = property.FindPropertyRelative(s_SerializedTypeField);
                return EditorGUI.GetPropertyHeight(serializedTypeProperty);
            }

            public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
            {
                SerializedProperty serializedTypeProperty = property.FindPropertyRelative(s_SerializedTypeField);
                EditorGUI.PropertyField(position, serializedTypeProperty, GUIContent.none);
            }
        }

        [CustomPropertyDrawer(typeof(ComponentAddedCriterion.SerializedTypeCollection))]
        private class TypedCriterionCollectionDrawer : CollectionWrapperDrawer
        {
            private const string k_FutureReferencePath = "FutureReference";

            protected override void OnListViewCreated(ListView listView)
            {
                base.OnListViewCreated(listView);
                listView.onAdd += view =>
                {
                    ++m_ListProperty.arraySize;
                    m_ListProperty.serializedObject.ApplyModifiedProperties();
                    SerializedProperty lastElement = m_ListProperty.GetArrayElementAtIndex(m_ListProperty.arraySize - 1);
                    lastElement.FindPropertyRelative(k_FutureReferencePath).objectReferenceValue = null;
                    m_ListProperty.serializedObject.ApplyModifiedProperties();
                };
            }
        }
    }
}
