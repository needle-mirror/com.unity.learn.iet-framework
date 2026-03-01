using UnityEditor;
using UnityEngine.UIElements;

namespace Unity.Tutorials.Editor
{
    /// <summary>
    /// Custom PropertyDrawer for <see cref="TypedCriterionCollection"/>.
    /// </summary>
    [CustomPropertyDrawer(typeof(TypedCriterionCollection))]
    internal class TypedCriterionCollectionDrawer : CollectionWrapperDrawer
    {
        private const string k_TypeNamePath = "Type.m_TypeName";
        private const string k_CriterionPropertyPath = "Criterion";

        protected override void OnListViewCreated(ListView listView)
        {
            listView.onAdd += view =>
            {
                ++m_ItemsProperty.arraySize;
                m_ItemsProperty.serializedObject.ApplyModifiedProperties();
                SerializedProperty lastElement = m_ItemsProperty.GetArrayElementAtIndex(m_ItemsProperty.arraySize - 1);
                lastElement.FindPropertyRelative(k_TypeNamePath).stringValue = "";
                lastElement.FindPropertyRelative(k_CriterionPropertyPath).objectReferenceValue = null;
                m_ItemsProperty.serializedObject.ApplyModifiedProperties();
            };

            base.OnListViewCreated(listView);
        }
    }
}
